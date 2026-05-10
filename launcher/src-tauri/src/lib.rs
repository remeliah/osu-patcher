use serde::{Deserialize, Serialize};
#[cfg(windows)]
use std::os::windows::process::CommandExt;
use std::{
    collections::BTreeMap,
    env, fs, io,
    path::{Path, PathBuf},
    process::{Command, Stdio},
};
use tauri::Manager;
use thiserror::Error;

const CONFIG_DIR_NAME: &str = "osuPatcher";
const CONFIG_FILE_NAME: &str = "config.ini";
const DEFAULT_SERVER: &str = "refx.online";
const PATCHER_ARTIFACT: &str = "_patcher\\bin\\Release\\_patcher.dll";
#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x08000000;
#[cfg(windows)]
const DETACHED_PROCESS: u32 = 0x00000008;

#[derive(Debug, Error)]
enum AppError {
    #[error("LOCALAPPDATA is not available")]
    MissingLocalAppData,
    #[error("failed to read {path}: {source}")]
    Read { path: String, source: io::Error },
    #[error("failed to write {path}: {source}")]
    Write { path: String, source: io::Error },
    #[error("failed to create {path}: {source}")]
    CreateDir { path: String, source: io::Error },
    #[error("failed to open {path}: {source}")]
    Open { path: String, source: io::Error },
    #[error("osu!.exe was not found")]
    OsuNotFound,
    #[error("failed to launch {path}: {source}")]
    Launch { path: String, source: io::Error },
    #[error("patcher-cli.exe was not found; build patcher-cli in Release first")]
    CliNotFound,
    #[error("_patcher.dll was not found; build the C# patcher in Release first")]
    PatcherNotFound,
    #[error("failed to copy {from} to {to}: {source}")]
    Copy {
        from: String,
        to: String,
        source: io::Error,
    },
    #[error("failed to query osu! process state: {0}")]
    ProcessQuery(io::Error),
}

impl From<AppError> for String {
    fn from(value: AppError) -> Self {
        value.to_string()
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct PatcherConfig {
    patch_relax: bool,
    transition_time: bool,
    performance_calculator: bool,
    path: String,
    artifact_path: String,
    artifact_exists: bool,
    created: bool,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct OsuState {
    path: Option<String>,
    running: bool,
}

impl Default for PatcherConfig {
    fn default() -> Self {
        Self {
            patch_relax: true,
            transition_time: true,
            performance_calculator: true,
            path: String::new(),
            artifact_path: String::new(),
            artifact_exists: false,
            created: false,
        }
    }
}

#[tauri::command]
fn load_config(app: tauri::AppHandle) -> Result<PatcherConfig, String> {
    load_config_inner(&app).map_err(Into::into)
}

#[tauri::command]
fn save_config(app: tauri::AppHandle, config: PatcherConfig) -> Result<PatcherConfig, String> {
    save_config_inner(&app, &config).map_err(Into::into)
}

#[tauri::command]
fn open_path(app: tauri::AppHandle, kind: String) -> Result<(), String> {
    open_path_inner(&app, &kind).map_err(Into::into)
}

#[tauri::command]
fn detect_osu() -> Result<OsuState, String> {
    detect_osu_inner().map_err(Into::into)
}

#[tauri::command]
fn launch_osu(
    app: tauri::AppHandle,
    path: Option<String>,
    server: Option<String>,
) -> Result<OsuState, String> {
    launch_osu_inner(&app, path, server).map_err(Into::into)
}

#[tauri::command]
fn inject_osu(
    app: tauri::AppHandle,
    path: Option<String>,
    server: Option<String>,
) -> Result<OsuState, String> {
    inject_osu_inner(&app, path, server).map_err(Into::into)
}

pub fn run() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            load_config,
            save_config,
            open_path,
            detect_osu,
            launch_osu,
            inject_osu,
        ])
        .run(tauri::generate_context!())
        .expect("failed to run osu! patcher app");
}

fn load_config_inner(app: &tauri::AppHandle) -> Result<PatcherConfig, AppError> {
    let config_path = config_path()?;
    let artifact_path = artifact_path(app);
    let mut created = false;

    if let Some(parent) = config_path.parent() {
        fs::create_dir_all(parent).map_err(|source| AppError::CreateDir {
            path: display_path(parent),
            source,
        })?;
    }

    let mut config = if config_path.exists() {
        parse_config(&config_path)?
    } else {
        created = true;
        let default = PatcherConfig::default();
        write_config(&config_path, &default)?;
        default
    };

    hydrate_paths(&mut config, &config_path, &artifact_path, created);
    Ok(config)
}

fn save_config_inner(
    app: &tauri::AppHandle,
    config: &PatcherConfig,
) -> Result<PatcherConfig, AppError> {
    let config_path = config_path()?;
    let artifact_path = artifact_path(app);

    if let Some(parent) = config_path.parent() {
        fs::create_dir_all(parent).map_err(|source| AppError::CreateDir {
            path: display_path(parent),
            source,
        })?;
    }

    write_config(&config_path, config)?;

    let mut saved = config.clone();
    hydrate_paths(&mut saved, &config_path, &artifact_path, false);
    Ok(saved)
}

fn open_path_inner(app: &tauri::AppHandle, kind: &str) -> Result<(), AppError> {
    let target = match kind {
        "config" => config_dir()?,
        "artifact" => artifact_path(app),
        _ => config_dir()?,
    };

    let path = if kind == "artifact" {
        target.parent().map(Path::to_path_buf).unwrap_or(target)
    } else {
        target
    };

    Command::new("explorer")
        .arg(&path)
        .spawn()
        .map_err(|source| AppError::Open {
            path: display_path(&path),
            source,
        })?;

    Ok(())
}

fn detect_osu_inner() -> Result<OsuState, AppError> {
    Ok(OsuState {
        path: find_osu_executable().map(|path| display_path(&path)),
        running: is_osu_running()?,
    })
}

fn launch_osu_inner(
    app: &tauri::AppHandle,
    path: Option<String>,
    server: Option<String>,
) -> Result<OsuState, AppError> {
    let exe = resolve_osu_path(path)?;
    let server = normalize_server(server);

    let mut command = Command::new(&exe);
    if let Some(parent) = exe.parent() {
        command.current_dir(parent);
    }

    command.arg("-devserver").arg(server);

    command.spawn().map_err(|source| AppError::Launch {
        path: display_path(&exe),
        source,
    })?;

    app.exit(0);

    Ok(OsuState {
        path: Some(display_path(&exe)),
        running: true,
    })
}

fn inject_osu_inner(
    app: &tauri::AppHandle,
    path: Option<String>,
    server: Option<String>,
) -> Result<OsuState, AppError> {
    let osu_path = resolve_osu_path(path)?;
    let patcher_path = resolve_patcher_artifact(app).ok_or(AppError::PatcherNotFound)?;
    prepare_patcher_dependencies(app, &patcher_path)?;
    let server = normalize_server(server);

    let cli_path = find_patcher_cli(app).ok_or(AppError::CliNotFound)?;
    let cli_dir = cli_path.parent().unwrap_or_else(|| Path::new("."));
    let cli_config = cli_dir.join("conf.db");
    fs::write(&cli_config, display_path(&osu_path)).map_err(|source| AppError::Write {
        path: display_path(&cli_config),
        source,
    })?;

    let mut command = Command::new(&cli_path);
    command
        .current_dir(cli_dir)
        .arg("--patcher")
        .arg(command_path(&patcher_path))
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::null());

    if !server.eq_ignore_ascii_case(DEFAULT_SERVER) {
        command.arg("--server").arg(server);
    }

    hide_console_window(&mut command);

    command.spawn().map_err(|source| AppError::Launch {
        path: display_path(&cli_path),
        source,
    })?;

    app.exit(0);

    Ok(OsuState {
        path: Some(display_path(&osu_path)),
        running: true,
    })
}

fn parse_config(path: &Path) -> Result<PatcherConfig, AppError> {
    let content = fs::read_to_string(path).map_err(|source| AppError::Read {
        path: display_path(path),
        source,
    })?;

    let entries = content
        .lines()
        .filter_map(|line| line.split_once('='))
        .map(|(key, value)| (key.trim().to_owned(), value.trim().to_owned()))
        .collect::<BTreeMap<_, _>>();

    Ok(PatcherConfig {
        patch_relax: read_bool(&entries, "PatchRelax", true),
        transition_time: read_bool(&entries, "TransitionTime", true),
        performance_calculator: read_bool(&entries, "PerformanceCalculator", true),
        ..PatcherConfig::default()
    })
}

fn write_config(path: &Path, config: &PatcherConfig) -> Result<(), AppError> {
    let content = format!(
        "PatchRelax={}\nTransitionTime={}\nPerformanceCalculator={}\n",
        config.patch_relax, config.transition_time, config.performance_calculator
    );

    fs::write(path, content).map_err(|source| AppError::Write {
        path: display_path(path),
        source,
    })
}

fn read_bool(entries: &BTreeMap<String, String>, key: &str, default: bool) -> bool {
    entries
        .get(key)
        .and_then(|value| value.parse::<bool>().ok())
        .unwrap_or(default)
}

fn hydrate_paths(
    config: &mut PatcherConfig,
    config_path: &Path,
    artifact_path: &Path,
    created: bool,
) {
    config.path = display_path(config_path);
    config.artifact_path = display_path(artifact_path);
    config.artifact_exists = artifact_path.exists();
    config.created = created;
}

fn config_path() -> Result<PathBuf, AppError> {
    Ok(config_dir()?.join(CONFIG_FILE_NAME))
}

fn config_dir() -> Result<PathBuf, AppError> {
    env::var_os("LOCALAPPDATA")
        .map(PathBuf::from)
        .map(|path| path.join(CONFIG_DIR_NAME))
        .ok_or(AppError::MissingLocalAppData)
}

fn find_osu_executable() -> Option<PathBuf> {
    let local_app_data = env::var_os("LOCALAPPDATA").map(PathBuf::from)?;
    let preferred = local_app_data.join("osu!").join("osu!.exe");
    if preferred.exists() {
        return Some(preferred);
    }

    fs::read_dir(local_app_data)
        .ok()?
        .filter_map(Result::ok)
        .map(|entry| entry.path().join("osu!.exe"))
        .find(|path| path.exists())
}

fn resolve_osu_path(path: Option<String>) -> Result<PathBuf, AppError> {
    path.filter(|value| !value.trim().is_empty())
        .map(PathBuf::from)
        .or_else(find_osu_executable)
        .filter(|path| path.exists())
        .ok_or(AppError::OsuNotFound)
}

fn normalize_server(server: Option<String>) -> String {
    server
        .map(|value| value.trim().to_owned())
        .filter(|value| !value.is_empty())
        .unwrap_or_else(|| DEFAULT_SERVER.to_owned())
}

fn find_patcher_cli(app: &tauri::AppHandle) -> Option<PathBuf> {
    resource_path(app, "patcher-cli.exe")
        .into_iter()
        .chain(repo_root().into_iter().flat_map(|root| {
            [
                root.join("patcher-cli\\patchershit\\bin\\Release\\patcher-cli.exe"),
                root.join("patcher-cli\\patchershit\\bin\\Debug\\patcher-cli.exe"),
                root.join("patcher-cli\\patchershit\\bin\\Release\\patchershit.exe"),
                root.join("patcher-cli\\patchershit\\bin\\Debug\\patchershit.exe"),
            ]
        }))
        .find(|path| path.exists())
}

fn resolve_patcher_artifact(app: &tauri::AppHandle) -> Option<PathBuf> {
    resource_path(app, "_patcher.dll")
        .into_iter()
        .chain(repo_root().into_iter().flat_map(|root| {
            [
                root.join("_patcher\\bin\\Release\\_patcher.dll"),
                root.join("_patcher\\bin\\Debug\\_patcher.dll"),
            ]
        }))
        .find(|path| path.exists())
}

fn prepare_patcher_dependencies(
    app: &tauri::AppHandle,
    patcher_path: &Path,
) -> Result<(), AppError> {
    let Some(patcher_dir) = patcher_path.parent() else {
        return Ok(());
    };

    let harmony_target = patcher_dir.join("0Harmony.dll");
    if !harmony_target.exists() {
        if let Some(harmony_source) = harmony_source(app) {
            fs::copy(&harmony_source, &harmony_target).map_err(|source| AppError::Copy {
                from: display_path(&harmony_source),
                to: display_path(&harmony_target),
                source,
            })?;
        }
    }

    let refx_target = patcher_dir.join("refx_ffi.dll");
    if !refx_target.exists() {
        if let Some(refx_source) = refx_ffi_source(app) {
            fs::copy(&refx_source, &refx_target).map_err(|source| AppError::Copy {
                from: display_path(&refx_source),
                to: display_path(&refx_target),
                source,
            })?;
        }
    }

    Ok(())
}

fn refx_ffi_source(app: &tauri::AppHandle) -> Option<PathBuf> {
    resource_path(app, "refx_ffi.dll")
        .into_iter()
        .chain(
            repo_root().map(|root| {
                root.join("refx-pp\\target\\i686-pc-windows-msvc\\release\\refx_ffi.dll")
            }),
        )
        .find(|path| path.exists())
}

fn harmony_source(app: &tauri::AppHandle) -> Option<PathBuf> {
    resource_path(app, "0Harmony.dll")
        .into_iter()
        .chain(
            repo_root()
                .map(|root| root.join("packages\\Lib.Harmony.2.3.3\\lib\\net472\\0Harmony.dll")),
        )
        .chain(repo_root().map(|root| root.join("_patcher\\bin\\Release\\0Harmony.dll")))
        .find(|path| path.exists())
}

fn is_osu_running() -> Result<bool, AppError> {
    let output = Command::new("tasklist")
        .args(["/FI", "IMAGENAME eq osu!.exe", "/FO", "CSV", "/NH"])
        .output()
        .map_err(AppError::ProcessQuery)?;

    let stdout = String::from_utf8_lossy(&output.stdout);
    Ok(stdout.lines().any(|line| line.contains("\"osu!.exe\"")))
}

fn artifact_path(app: &tauri::AppHandle) -> PathBuf {
    resource_path(app, "_patcher.dll")
        .or_else(|| {
            repo_root()
                .map(|root| root.join(PATCHER_ARTIFACT))
                .filter(|path| path.exists())
        })
        .unwrap_or_else(|| PathBuf::from(PATCHER_ARTIFACT))
}

fn repo_root() -> Option<PathBuf> {
    let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    manifest_dir
        .parent()
        .and_then(Path::parent)
        .map(Path::to_path_buf)
}

fn resource_path(app: &tauri::AppHandle, name: &str) -> Option<PathBuf> {
    app.path()
        .resolve(name, tauri::path::BaseDirectory::Resource)
        .ok()
}

fn display_path(path: &Path) -> String {
    path.to_string_lossy().into_owned()
}

fn command_path(path: &Path) -> String {
    let path = display_path(path);

    if let Some(stripped) = path.strip_prefix(r"\\?\UNC\") {
        format!(r"\\{}", stripped)
    } else if let Some(stripped) = path.strip_prefix(r"\\?\") {
        stripped.to_owned()
    } else {
        path
    }
}

#[cfg(windows)]
fn hide_console_window(command: &mut Command) {
    command.creation_flags(CREATE_NO_WINDOW | DETACHED_PROCESS);
}

#[cfg(not(windows))]
fn hide_console_window(_: &mut Command) {}
