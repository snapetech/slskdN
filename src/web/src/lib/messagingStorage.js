import {
  getLocalStorageItem,
  removeLocalStorageItem,
  setLocalStorageItem,
} from './storage';

export const V1_KEY = 'slskd-messaging-workspace';
export const V2_KEY = 'slskd-messaging-workspace.v2';

export const ZOOM_LEVELS = ['s', 'm', 'l', 'xl'];
export const TAB_TYPES = new Set(['chat', 'room', 'pod']);

const DEFAULT_TREE_WIDTH = 240;
const DEFAULT_MEMBER_WIDTH = 200;
const TREE_WIDTH_BOUNDS = { max: 480, min: 160 };
const MEMBER_WIDTH_BOUNDS = { max: 360, min: 120 };

const isObject = (value) =>
  value && typeof value === 'object' && !Array.isArray(value);

const clamp = (value, { min, max }) => Math.min(max, Math.max(min, value));

const DEFAULT_ZOOM = 's';

const sanitizeZoom = (value) =>
  typeof value === 'string' && ZOOM_LEVELS.includes(value) ? value : DEFAULT_ZOOM;

const sanitizeTab = (tab, indexFallback) => {
  if (
    !isObject(tab) ||
    typeof tab.type !== 'string' ||
    typeof tab.target !== 'string' ||
    !TAB_TYPES.has(tab.type)
  ) {
    return null;
  }

  const id =
    typeof tab.id === 'string' && tab.id.length > 0
      ? tab.id
      : `${tab.type}-${indexFallback + 1}`;

  return {
    id,
    label: typeof tab.label === 'string' ? tab.label : undefined,
    target: tab.target,
    type: tab.type,
  };
};

const sanitizePinned = (entry) => {
  if (
    !isObject(entry) ||
    typeof entry.type !== 'string' ||
    typeof entry.target !== 'string' ||
    !TAB_TYPES.has(entry.type)
  ) {
    return null;
  }

  return { target: entry.target, type: entry.type };
};

const sanitizePaneSettings = (settings) => {
  const safe = isObject(settings) ? settings : {};
  return {
    memberRailOpenByTarget: isObject(safe.memberRailOpenByTarget)
      ? safe.memberRailOpenByTarget
      : {},
    memberWidth: clamp(
      Number.isFinite(safe.memberWidth) ? safe.memberWidth : DEFAULT_MEMBER_WIDTH,
      MEMBER_WIDTH_BOUNDS,
    ),
    treeWidth: clamp(
      Number.isFinite(safe.treeWidth) ? safe.treeWidth : DEFAULT_TREE_WIDTH,
      TREE_WIDTH_BOUNDS,
    ),
  };
};

const sanitizeCollapsedSections = (sections) => {
  const safe = isObject(sections) ? sections : {};
  return {
    meshPods: safe.meshPods === true,
    soulseekDirect: safe.soulseekDirect === true,
    soulseekRooms: safe.soulseekRooms === true,
  };
};

export const defaultWorkspace = () => ({
  activeTabId: null,
  collapsedSections: sanitizeCollapsedSections(null),
  paneSettings: sanitizePaneSettings(null),
  pinned: [],
  tabCounter: 0,
  tabs: [],
  version: 2,
  zoom: DEFAULT_ZOOM,
});

const sanitizeWorkspace = (raw) => {
  if (!isObject(raw)) return defaultWorkspace();

  const tabs = Array.isArray(raw.tabs)
    ? raw.tabs.map(sanitizeTab).filter(Boolean)
    : [];
  const pinned = Array.isArray(raw.pinned)
    ? raw.pinned.map(sanitizePinned).filter(Boolean)
    : [];
  const tabIds = new Set(tabs.map((tab) => tab.id));
  const activeTabId =
    typeof raw.activeTabId === 'string' && tabIds.has(raw.activeTabId)
      ? raw.activeTabId
      : tabs[0]?.id ?? null;
  const tabCounter = Math.max(
    Number.isInteger(raw.tabCounter) ? raw.tabCounter : 0,
    tabs.length,
  );

  return {
    activeTabId,
    collapsedSections: sanitizeCollapsedSections(raw.collapsedSections),
    paneSettings: sanitizePaneSettings(raw.paneSettings),
    pinned,
    tabCounter,
    tabs,
    version: 2,
    zoom: sanitizeZoom(raw.zoom),
  };
};

const migrateV1 = (rawV1) => {
  if (!isObject(rawV1)) return null;

  const panels = Array.isArray(rawV1.panels) ? rawV1.panels : [];
  const tabs = panels
    .map((panel, index) => sanitizeTab(panel, index))
    .filter(Boolean);

  if (tabs.length === 0 && !Number.isInteger(rawV1.panelCounter)) return null;

  const firstActive = panels.find((panel) => panel && panel.collapsed !== true);
  const activeTabId =
    tabs.find((tab) => tab.target === firstActive?.target && tab.type === firstActive?.type)?.id ??
    tabs[0]?.id ??
    null;

  return sanitizeWorkspace({
    ...defaultWorkspace(),
    activeTabId,
    tabCounter: Number.isInteger(rawV1.panelCounter) ? rawV1.panelCounter : tabs.length,
    tabs,
  });
};

export const loadWorkspace = () => {
  const v2Raw = getLocalStorageItem(V2_KEY);
  if (v2Raw) {
    try {
      return sanitizeWorkspace(JSON.parse(v2Raw));
    } catch {
      // fall through to default
    }
  }

  const v1Raw = getLocalStorageItem(V1_KEY);
  if (v1Raw) {
    try {
      const migrated = migrateV1(JSON.parse(v1Raw));
      if (migrated) {
        saveWorkspace(migrated);
        // keep v1 around until the user is fully on v2; clear once in steady state
        return migrated;
      }
    } catch {
      // fall through to default
    }
  }

  return defaultWorkspace();
};

export const saveWorkspace = (workspace) => {
  const sanitized = sanitizeWorkspace(workspace);
  setLocalStorageItem(V2_KEY, JSON.stringify(sanitized));
  return sanitized;
};

export const clearLegacyWorkspace = () => removeLocalStorageItem(V1_KEY);

export const makeTabId = (counter, type) => `${type}-${counter}`;

export const TREE_WIDTH_RANGE = TREE_WIDTH_BOUNDS;
export const MEMBER_WIDTH_RANGE = MEMBER_WIDTH_BOUNDS;
