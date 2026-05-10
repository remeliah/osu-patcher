import { invoke } from "@tauri-apps/api/core";
import "./styles.css";

const PATCHES = [
  {
    key: "patchRelax",
    label: "Relax / Autopilot patch",
    description:
      "Removes relax and autopilot limitations so misses, combo break sounds, and ranking panels stay visible.",
  },
  {
    key: "transitionTime",
    label: "Fast transitions",
    description: "Shortens screen fade transitions for faster menu and gameplay flow.",
  },
  {
    key: "performanceCalculator",
    label: "Performance calculator",
    description: "Enables in-game performance calculation for plays.",
  },
];

const DEFAULT_SERVER = "refx.online";

const state = {
  config: null,
  osu: null,
  osuPathInput: "",
  serverInput: DEFAULT_SERVER,
  status: "Loading patcher state...",
  busy: false,
};

const app = document.querySelector("#app");

function render() {
  app.innerHTML = `
    <div class="background-grid" aria-hidden="true">
      ${Array.from({ length: 18 }, (_, index) => `<span class="tri tri-${index + 1}"></span>`).join("")}
    </div>
    <section class="shell">
      <header class="topbar">
        <div>
          <p class="eyebrow">osu! patcher</p>
          <h1>Patch controls</h1>
        </div>
        <button class="icon-button" id="refresh" title="Refresh state" aria-label="Refresh state">
          <span class="refresh-mark" aria-hidden="true"></span>
        </button>
      </header>

      <section class="status-panel">
        <div>
          <span class="status-dot ${state.busy ? "busy" : ""}"></span>
          <span>${escapeHtml(state.status)}</span>
        </div>
        <code>${state.config ? escapeHtml(state.config.path) : "config.ini"}</code>
      </section>

      <section class="launch-panel">
        <div class="launch-copy">
          <h2>Patch osu!</h2>
          <p>${state.osu?.running ? "Running" : "Not running"}</p>
        </div>
        <div class="path-field">
          <label>
            <span>osu!.exe</span>
            <input id="osu-path" type="text" value="${escapeAttribute(state.osuPathInput)}" placeholder="C:\\Users\\...\\AppData\\Local\\osu!\\osu!.exe" ${state.busy ? "disabled" : ""} />
          </label>
          <label>
            <span>Server</span>
            <input id="server" type="text" list="server-presets" value="${escapeAttribute(state.serverInput)}" ${state.busy ? "disabled" : ""} />
            <datalist id="server-presets">
              <option value="refx.online"></option>
              <option value="remeliah.cyou"></option>
            </datalist>
          </label>
          <button id="patch-osu" class="primary" ${state.busy ? "disabled" : ""}>Patch and launch</button>
        </div>
      </section>

      <section class="patch-list">
        ${PATCHES.map((patch) => renderPatch(patch)).join("")}
      </section>
    </section>
  `;

  document.querySelector("#refresh").addEventListener("click", loadConfig);
  document.querySelector("#patch-osu").addEventListener("click", patchAndLaunchOsu);
  document.querySelector("#osu-path").addEventListener("input", (event) => {
    state.osuPathInput = event.target.value;
  });
  document.querySelector("#server").addEventListener("input", (event) => {
    state.serverInput = event.target.value;
  });

  for (const patch of PATCHES) {
    const input = document.querySelector(`#${patch.key}`);
    input?.addEventListener("change", (event) => {
      state.config[patch.key] = event.target.checked;
      saveConfigSilently();
    });
  }
}

function renderPatch(patch) {
  const checked = state.config?.[patch.key] ?? false;
  return `
    <label class="patch-row" for="${patch.key}">
      <span>
        <strong>${escapeHtml(patch.label)}</strong>
        <small>${escapeHtml(patch.description)}</small>
      </span>
      <input id="${patch.key}" type="checkbox" ${checked ? "checked" : ""} ${state.busy || !state.config ? "disabled" : ""} />
    </label>
  `;
}

async function loadConfig() {
  await run("Loading patcher state...", async () => {
    const [config, osu] = await Promise.all([invoke("load_config"), invoke("detect_osu")]);
    state.config = config;
    state.osu = osu;
    state.osuPathInput = state.osuPathInput || osu.path || "";
    state.status = state.config.created ? "Created default config" : "Config loaded";
  });
}

async function saveConfig() {
  if (!state.config) return;

  await run("Saving config...", async () => {
    state.config = await invoke("save_config", { config: state.config });
    state.status = "Config saved";
  });
}

async function saveConfigSilently() {
  if (!state.config || state.busy) return;

  state.busy = true;
  state.status = "Saving config...";
  render();

  try {
    state.config = await invoke("save_config", { config: state.config });
    state.status = "Config saved";
  } catch (error) {
    state.status = String(error);
  } finally {
    state.busy = false;
    render();
  }
}

async function patchAndLaunchOsu() {
  await run("Patching and launching osu!...", async () => {
    if (state.config) {
      state.config = await invoke("save_config", { config: state.config });
    }

    state.osu = await invoke("inject_osu", {
      path: state.osuPathInput.trim() || null,
      server: state.serverInput.trim() || DEFAULT_SERVER,
    });
    state.osuPathInput = state.osu.path || state.osuPathInput;
    state.status = "osu! launched and patcher injected";
  });
}

async function run(message, operation) {
  state.busy = true;
  state.status = message;
  render();

  try {
    await operation();
  } catch (error) {
    state.status = String(error);
  } finally {
    state.busy = false;
    render();
  }
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function escapeAttribute(value) {
  return escapeHtml(value).replaceAll("`", "&#096;");
}

render();
loadConfig();
