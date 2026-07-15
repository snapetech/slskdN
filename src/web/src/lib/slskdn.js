import api from './api';

// Helper to safely call API endpoints that may not exist yet
const safeGet = async (endpoint, fallback = null) => {
  try {
    const response = await api.get(endpoint);
    return response.data;
  } catch (error) {
    // Return fallback for 404s or other errors - endpoint may not be implemented
    if (error?.response?.status === 404) {
      console.debug(
        `Endpoint ${endpoint} not found (expected during development)`,
      );
    }

    return fallback;
  }
};

// Capabilities API
export const getCapabilities = async () => {
  return safeGet('/capabilities', { features: [] });
};

export const getDiscoveredPeers = async () => {
  return safeGet('/capabilities/peers', { peers: [] });
};

// HashDatabase API
export const getHashDatabaseStats = async () => {
  return safeGet('/hashdb/stats', { currentSeqId: 0, totalHashEntries: 0 });
};

export const getHashDatabaseEntries = async (limit = 100, offset = 0) => {
  return safeGet(`/hashdb/entries?limit=${limit}&offset=${offset}`, {
    entries: [],
  });
};

// Mesh API
export const getMeshStats = async () => {
  return safeGet('/mesh/stats', {
    currentSeqId: 0,
    isSyncing: false,
    knownMeshPeers: 0,
  });
};

export const getMeshPeers = async () => {
  return safeGet('/mesh/peers', { count: 0, peers: [] });
};

export const triggerMeshSync = async (username) => {
  try {
    return (await api.post(`/mesh/sync/${encodeURIComponent(username)}`)).data;
  } catch (error) {
    return { error: error?.message || 'Sync failed', success: false };
  }
};

// Backfill API
export const getBackfillStats = async () => {
  return safeGet('/backfill/stats', { isActive: false, isRunning: false });
};

export const getBackfillCandidates = async (limit = 50) => {
  return safeGet(`/backfill/candidates?limit=${limit}`, { candidates: [] });
};

export const backfillFromSearchHistory = async (options = {}) => {
  try {
    const searchParameters = new URLSearchParams();

    if (options.batchSize) {
      searchParameters.append('batchSize', options.batchSize);
    }

    if (options.reset) {
      searchParameters.append('reset', 'true');
    }

    const query = searchParameters.toString()
      ? `?${searchParameters.toString()}`
      : '';

    return (await api.post(`/hashdb/backfill/from-history${query}`)).data;
  } catch (error) {
    return { error: error?.message || 'Backfill failed', success: false };
  }
};

// MultiSource API
export const getActiveSwarmJobs = async () => {
  return safeGet('/multisource/jobs', []);
};

export const getSwarmJob = async (jobId) => {
  return safeGet(`/multisource/jobs/${encodeURIComponent(jobId)}`, null);
};

// DHT API
export const getDhtStatus = async () => {
  const dht = await safeGet('/dht/status', {
    dhtNodeCount: 0,
    isLanOnly: false,
    isBeaconCapable: false,
    isDhtRunning: false,
    verifiedBeaconCount: 0,
  });

  return {
    ...dht,
    isLanOnly: dht.isLanOnly ?? dht.lanOnly ?? false,
  };
};

const asArray = (value) => (Array.isArray(value) ? value : []);

const parseCapabilities = ({ capabilitiesJson, capabilitiesVersion }) => {
  let document = {};
  if (typeof capabilitiesJson === 'string') {
    try {
      document = JSON.parse(capabilitiesJson);
    } catch {
      document = {};
    }
  }

  return {
    features: asArray(document.features),
    version: document.version || capabilitiesVersion || 'slskdN',
  };
};

const normalizeSwarmJob = (job = {}) => {
  const completedChunks = Number(job.completedChunks) || 0;
  const totalChunks = Number(job.totalChunks) || 0;

  return {
    ...job,
    activeSources: job.activeSources ?? job.activeWorkers ?? 0,
    downloadedBytes: job.downloadedBytes ?? job.bytesDownloaded ?? 0,
    jobId: job.jobId ?? job.id,
    progressPercent:
      job.progressPercent ??
      (totalChunks > 0 ? (completedChunks * 100) / totalChunks : 0),
    totalBytes: job.totalBytes ?? job.fileSize ?? 0,
    workers: asArray(job.workers),
  };
};

const emptyNetworkStats = () => ({
  backfill: { isActive: false },
  capabilities: null,
  dht: null,
  discoveredPeers: [],
  hashDb: { currentSeqId: 0, totalEntries: 0 },
  mesh: {
    connectedPeerCount: 0,
    isSyncing: false,
    localSeqId: 0,
    warnings: [],
  },
  meshPeers: [],
  swarmJobs: [],
  transport: null,
});

// eslint-disable-next-line complexity
const normalizeNetworkStats = (snapshot) => {
  if (!snapshot || typeof snapshot !== 'object' || Array.isArray(snapshot)) {
    return emptyNetworkStats();
  }

  const rawBackfill = snapshot.backfill || {};
  const rawDht = snapshot.dht || {};
  const rawHashDatabase = snapshot.hashDb || {};
  const rawMesh = snapshot.mesh || {};
  const rawTransport = snapshot.transport || {};
  const totalFlacEntries = Number(rawHashDatabase.totalFlacEntries) || 0;
  const hashedFlacEntries = Number(rawHashDatabase.hashedFlacEntries) || 0;

  return {
    backfill: {
      ...rawBackfill,
      isActive:
        rawBackfill.isActive ??
        rawBackfill.isRunning ??
        (Number(rawBackfill.active) || 0) > 0,
    },
    capabilities: parseCapabilities(snapshot),
    dht: {
      ...rawDht,
      isLanOnly: rawDht.isLanOnly ?? rawDht.lanOnly ?? false,
    },
    discoveredPeers: asArray(snapshot.discoveredPeers).map((peer) => ({
      ...peer,
      lastSeenAt: peer.lastSeenAt ?? peer.lastSeen,
      version: peer.version ?? peer.clientVersion,
    })),
    hashDb: {
      ...rawHashDatabase,
      coveragePercent:
        rawHashDatabase.coveragePercent ??
        (totalFlacEntries > 0
          ? (hashedFlacEntries * 100) / totalFlacEntries
          : undefined),
      currentSeqId: rawHashDatabase.currentSeqId ?? 0,
      dbSizeBytes:
        rawHashDatabase.dbSizeBytes ??
        rawHashDatabase.databaseSizeBytes ??
        0,
      totalEntries:
        rawHashDatabase.totalHashEntries ??
        rawHashDatabase.totalEntries ??
        0,
    },
    mesh: {
      ...rawMesh,
      connectedPeerCount:
        rawMesh.knownMeshPeers ?? rawMesh.connectedPeerCount ?? 0,
      isSyncing: rawMesh.isSyncing ?? false,
      localSeqId: rawMesh.currentSeqId ?? rawMesh.localSeqId ?? 0,
      warnings: asArray(rawMesh.warnings),
    },
    meshPeers: asArray(snapshot.meshPeers).map((peer) => ({
      ...peer,
      lastSeqId: peer.lastSeqId ?? peer.latestSeqId,
      lastSyncAt: peer.lastSyncAt ?? peer.lastSyncTime,
    })),
    swarmJobs: asArray(snapshot.swarmJobs).map(normalizeSwarmJob),
    transport: {
      dht: rawTransport.dht ?? rawTransport.activeDhtSessions ?? 0,
      natType:
        rawTransport.natType ?? rawTransport.detectedNatType ?? 'Unknown',
      overlay:
        rawTransport.overlay ?? rawTransport.activeOverlaySessions ?? 0,
    },
  };
};

export const getNetworkStats = async ({ includePeers = false } = {}) => {
  const query = includePeers ? '?includePeers=true' : '';
  return safeGet(`/network/stats${query}`, null);
};

// One server-side summary request shared by the footer and Network dashboard.
export const getSlskdnStats = async ({ includePeers = false } = {}) => {
  const snapshot = await getNetworkStats({ includePeers });
  return normalizeNetworkStats(snapshot);
};
