import './MessagingV2.css';
import * as chat from '../../lib/chat';
import {
  loadWorkspace,
  makeTabId,
  MEMBER_WIDTH_RANGE,
  saveWorkspace,
  TREE_WIDTH_RANGE,
  ZOOM_LEVELS,
} from '../../lib/messagingStorage';
import * as pods from '../../lib/pods';
import * as rooms from '../../lib/rooms';
import Composer from './Composer';
import {
  asArray,
  channelLabel,
  decodePodTarget,
  encodePodTarget,
  isPodDirectChannel,
} from './Messaging';
import {
  createChatAdapter,
  createPodAdapter,
  createRoomAdapter,
} from './messagingAdapters';
import CommandHelp from './CommandHelp';
import MessageStream from './MessageStream';
import QuickSwitcher from './QuickSwitcher';
import UserPopover from './UserPopover';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useNavigate, useParams } from 'react-router-dom';

const NETWORKS = ['all', 'soulseek', 'mesh'];

const COMPOSER_COMMANDS = [
  {
    description: 'Send an italic action (e.g. /me waves).',
    name: 'me',
    syntax: '/me <action>',
  },
  {
    description: 'Open a direct-message tab with the given user.',
    name: 'msg',
    syntax: '/msg <user>',
  },
  {
    description: 'Join (or create) a Soulseek room.',
    name: 'join',
    syntax: '/join <room>',
  },
  {
    aliases: ['part', 'leave'],
    description: 'Leave the current room or pod, or close the current DM tab.',
    name: 'close',
    syntax: '/close',
  },
  {
    description: 'Set message density.',
    name: 'zoom',
    syntax: '/zoom <s|m|l|xl>',
  },
  {
    description: 'Refetch messages for the current tab.',
    name: 'reload',
    syntax: '/reload',
  },
  {
    description: 'Open the channel switcher.',
    name: 'switch',
    syntax: '/switch',
  },
  {
    description: 'Show this command list.',
    name: 'help',
    syntax: '/help',
  },
];

const tabAccent = (tab) => (tab.type === 'pod' ? 'mesh' : 'slsk');

const GOLD_STAR_CLUB_POD_ID = 'pod:901d57a2c1bb4e5d90d57a2c1bb4e5d0';

const tabLabel = (tab) => {
  if (tab.type === 'room') return `#${tab.target}`;
  if (tab.type === 'pod') return tab.label || 'Pod channel';
  return `@${tab.target}`;
};

const tabSubtitle = (tab) => {
  if (tab.type === 'room') return 'Soulseek room';
  if (tab.type === 'pod') return 'Mesh channel';
  return 'Soulseek DM';
};

const formatUnread = (count) => {
  if (!Number.isFinite(count) || count <= 0) return null;
  return count > 99 ? '99+' : String(count);
};

const zoomIndex = (zoom) => Math.max(0, ZOOM_LEVELS.indexOf(zoom));

const MessagingV2 = ({ initialKind = 'mixed', state }) => {
  const navigate = useNavigate();
  const params = useParams();

  const [workspace, setWorkspace] = useState(loadWorkspace);
  const persistTimer = useRef(null);

  const persistWorkspace = useCallback((next) => {
    if (persistTimer.current) {
      window.clearTimeout(persistTimer.current);
    }
    persistTimer.current = window.setTimeout(() => {
      saveWorkspace(next);
    }, 200);
  }, []);

  const updateWorkspace = useCallback(
    (mutator) => {
      setWorkspace((previous) => {
        const next = mutator(previous);
        if (next === previous) return previous;
        persistWorkspace(next);
        return next;
      });
    },
    [persistWorkspace],
  );

  useEffect(() => () => {
    if (persistTimer.current) window.clearTimeout(persistTimer.current);
    if (roomDirectoryRetryTimer.current) {
      window.clearTimeout(roomDirectoryRetryTimer.current);
    }
  }, []);

  const [conversations, setConversations] = useState([]);
  const [joinedRooms, setJoinedRooms] = useState([]);
  const [availableRooms, setAvailableRooms] = useState([]);
  const [podChannels, setPodChannels] = useState([]);
  const [discoveredPods, setDiscoveredPods] = useState([]);
  const [networkFilter, setNetworkFilter] = useState('all');
  const [memberRailOpen, setMemberRailOpen] = useState(true);
  const [adapterMembers, setAdapterMembers] = useState([]);
  const [qsOpen, setQsOpen] = useState(false);
  const [helpOpen, setHelpOpen] = useState(false);
  const [streamReloadToken, setStreamReloadToken] = useState(0);
  const [dmDraft, setDmDraft] = useState('');
  const [roomDraft, setRoomDraft] = useState('');
  const [podDraft, setPodDraft] = useState('');
  const [dmAddOpen, setDmAddOpen] = useState(false);
  const [roomAddOpen, setRoomAddOpen] = useState(false);
  const [roomJoinError, setRoomJoinError] = useState('');
  const [podAddOpen, setPodAddOpen] = useState(false);
  const [userPopover, setUserPopover] = useState(null);
  const [composerDraft, setComposerDraft] = useState('');
  const composerInputRef = useRef(null);
  const lastWheelZoomAt = useRef(0);
  const roomDirectoryRetryTimer = useRef(null);
  const roomDirectoryRetryCount = useRef(0);
  const currentUser = state?.user?.username;

  const hydrate = useCallback(async () => {
    const [
      serverConversations,
      serverJoinedRooms,
      serverPods,
      serverDiscoveredPods,
    ] = await Promise.all([
      chat.getAll(),
      rooms.getJoined(),
      pods.list().catch(() => []),
      Promise.resolve(
        typeof pods.discoverAll === 'function' ? pods.discoverAll(50) : [],
      ).catch(() => []),
    ]);
    const podDetails = await Promise.all(
      asArray(serverPods)
        .filter((pod) => pod && typeof pod === 'object' && !Array.isArray(pod))
        .map(async (pod) => {
          try {
            return await pods.get(pod.podId);
          } catch {
            return pod;
          }
        }),
    );

    setConversations(
      asArray(serverConversations)
        .filter((c) => c && typeof c === 'object' && !Array.isArray(c) && c.username)
        .sort((a, b) => {
          if (a.hasUnAcknowledgedMessages !== b.hasUnAcknowledgedMessages) {
            return a.hasUnAcknowledgedMessages ? -1 : 1;
          }
          return a.username.localeCompare(b.username);
        }),
    );
    setJoinedRooms(asArray(serverJoinedRooms).filter(Boolean).sort());
    setDiscoveredPods(
      asArray(serverDiscoveredPods)
        .filter((pod) => pod && typeof pod === 'object' && !Array.isArray(pod))
        .sort((a, b) =>
          (a.name || a.Name || a.podId || a.PodId || '').localeCompare(
            b.name || b.Name || b.podId || b.PodId || '',
          )),
    );
    setPodChannels(
      podDetails
        .flatMap((pod) =>
          asArray(pod.channels)
            .filter((channel) =>
              channel && typeof channel === 'object' && !Array.isArray(channel))
            .map((channel) => ({
              channelId: channel.channelId,
              channelKind: channel.kind,
              channelName: channel.name,
              podId: pod.podId,
              podName: pod.name || pod.podId,
              target: encodePodTarget(pod.podId, channel.channelId),
            })),
        )
        .sort((a, b) => channelLabel(a).localeCompare(channelLabel(b))),
    );
  }, []);

  const loadAvailableRooms = useCallback(async () => {
    const serverAvailableRooms = await Promise.resolve(rooms.getAvailable()).catch(() => null);
    const nextAvailableRooms = asArray(serverAvailableRooms)
        .map((room) => (typeof room === 'string' ? room : room?.name || room?.Name || ''))
        .filter(Boolean)
        .sort((a, b) => a.localeCompare(b));

    setAvailableRooms((previous) => {
      if (nextAvailableRooms.length > 0) {
        roomDirectoryRetryCount.current = 0;
        return nextAvailableRooms;
      }

      if (previous.length > 0) {
        return previous;
      }

      if (roomAddOpen && roomDirectoryRetryCount.current < 4 && !roomDirectoryRetryTimer.current) {
        roomDirectoryRetryCount.current += 1;
        roomDirectoryRetryTimer.current = window.setTimeout(() => {
          roomDirectoryRetryTimer.current = null;
          loadAvailableRooms().catch((error) => {
            console.error('Failed to retry available rooms:', error);
          });
        }, 1_500);
      }

      return nextAvailableRooms;
    });
  }, [roomAddOpen]);

  useEffect(() => {
    hydrate().catch((error) => {
      console.error('Failed to hydrate v2 messaging workspace:', error);
    });
    const interval = window.setInterval(() => {
      hydrate().catch((error) => {
        console.error('Failed to hydrate v2 messaging workspace:', error);
      });
    }, 10_000);
    return () => window.clearInterval(interval);
  }, [hydrate]);

  useEffect(() => {
    if (!roomAddOpen) return;
    roomDirectoryRetryCount.current = 0;
    loadAvailableRooms().catch((error) => {
      console.error('Failed to load available rooms:', error);
    });
  }, [loadAvailableRooms, roomAddOpen]);

  const visiblePodChannels = useMemo(
    () => podChannels.filter((channel) => !isPodDirectChannel(channel)),
    [podChannels],
  );

  const openTab = useCallback(
    (type, target, label) => {
      const trimmed = `${target || ''}`.trim();
      if (!trimmed) return;
      updateWorkspace((previous) => {
        const existing = previous.tabs.find(
          (tab) => tab.type === type && tab.target === trimmed,
        );
        if (existing) {
          if (previous.activeTabId === existing.id) return previous;
          return { ...previous, activeTabId: existing.id };
        }
        const counter = previous.tabCounter + 1;
        const id = makeTabId(counter, type);
        const newTab = { id, label, target: trimmed, type };
        return {
          ...previous,
          activeTabId: id,
          tabCounter: counter,
          tabs: [...previous.tabs, newTab],
        };
      });
    },
    [updateWorkspace],
  );

  const closeTab = useCallback(
    (tabId) => {
      updateWorkspace((previous) => {
        const tabs = previous.tabs.filter((tab) => tab.id !== tabId);
        let activeTabId = previous.activeTabId;
        if (activeTabId === tabId) {
          const closedIndex = previous.tabs.findIndex((tab) => tab.id === tabId);
          activeTabId = tabs[closedIndex]?.id ?? tabs[closedIndex - 1]?.id ?? null;
        }
        return { ...previous, activeTabId, tabs };
      });
    },
    [updateWorkspace],
  );

  const activateTab = useCallback(
    (tabId) => {
      updateWorkspace((previous) =>
        previous.activeTabId === tabId ? previous : { ...previous, activeTabId: tabId },
      );
    },
    [updateWorkspace],
  );

  const setZoom = useCallback(
    (zoom) => {
      if (!ZOOM_LEVELS.includes(zoom)) return;
      updateWorkspace((previous) =>
        previous.zoom === zoom ? previous : { ...previous, zoom },
      );
    },
    [updateWorkspace],
  );

  const adjustZoom = useCallback(
    (delta) => {
      updateWorkspace((previous) => {
        const currentIndex = zoomIndex(previous.zoom);
        const nextIndex = Math.min(
          ZOOM_LEVELS.length - 1,
          Math.max(0, currentIndex + delta),
        );
        const nextZoom = ZOOM_LEVELS[nextIndex];
        return previous.zoom === nextZoom ? previous : { ...previous, zoom: nextZoom };
      });
    },
    [updateWorkspace],
  );

  const handleWheelZoom = useCallback(
    (event) => {
      if (!event.ctrlKey && !event.metaKey) return;
      event.preventDefault();
      const now = Date.now();
      if (now - lastWheelZoomAt.current < 90) return;
      lastWheelZoomAt.current = now;
      adjustZoom(event.deltaY < 0 ? 1 : -1);
    },
    [adjustZoom],
  );

  const toggleSection = useCallback(
    (section) => {
      updateWorkspace((previous) => ({
        ...previous,
        collapsedSections: {
          ...previous.collapsedSections,
          [section]: !previous.collapsedSections[section],
        },
      }));
    },
    [updateWorkspace],
  );

  const setTreeWidth = useCallback(
    (width) => {
      const clamped = Math.min(
        TREE_WIDTH_RANGE.max,
        Math.max(TREE_WIDTH_RANGE.min, Math.round(width)),
      );
      updateWorkspace((previous) =>
        previous.paneSettings.treeWidth === clamped
          ? previous
          : {
              ...previous,
              paneSettings: { ...previous.paneSettings, treeWidth: clamped },
            },
      );
    },
    [updateWorkspace],
  );

  const setMemberWidth = useCallback(
    (width) => {
      const clamped = Math.min(
        MEMBER_WIDTH_RANGE.max,
        Math.max(MEMBER_WIDTH_RANGE.min, Math.round(width)),
      );
      updateWorkspace((previous) =>
        previous.paneSettings.memberWidth === clamped
          ? previous
          : {
              ...previous,
              paneSettings: { ...previous.paneSettings, memberWidth: clamped },
            },
      );
    },
    [updateWorkspace],
  );

  const treeResize = useDragResize({
    direction: 'right',
    onChange: setTreeWidth,
    range: TREE_WIDTH_RANGE,
    value: workspace.paneSettings.treeWidth,
  });
  const memberResize = useDragResize({
    direction: 'left',
    onChange: setMemberWidth,
    range: MEMBER_WIDTH_RANGE,
    value: workspace.paneSettings.memberWidth,
  });

  const deleteConversation = useCallback(
    async (username) => {
      if (!username) return;
      if (!window.confirm(`Permanently delete the saved message thread with "${username}"?`)) {
        return;
      }
      try {
        await chat.remove({ username });
        await hydrate();
        updateWorkspace((previous) => {
          const tabs = previous.tabs.filter(
            (tab) => !(tab.type === 'chat' && tab.target === username),
          );
          return tabs.length === previous.tabs.length
            ? previous
            : {
                ...previous,
                activeTabId: tabs.find((tab) => tab.id === previous.activeTabId)
                  ? previous.activeTabId
                  : tabs[0]?.id ?? null,
                tabs,
              };
        });
      } catch (error) {
        console.error('Failed to delete conversation:', error);
      }
    },
    [hydrate, updateWorkspace],
  );

  const leaveRoom = useCallback(
    async (roomName) => {
      if (!roomName) return;
      if (!window.confirm(`Leave room "${roomName}"?`)) return;
      try {
        await rooms.leave({ roomName });
        await hydrate();
        updateWorkspace((previous) => {
          const tabs = previous.tabs.filter(
            (tab) => !(tab.type === 'room' && tab.target === roomName),
          );
          return tabs.length === previous.tabs.length
            ? previous
            : {
                ...previous,
                activeTabId: tabs.find((tab) => tab.id === previous.activeTabId)
                  ? previous.activeTabId
                  : tabs[0]?.id ?? null,
                tabs,
              };
        });
      } catch (error) {
        console.error('Failed to leave room:', error);
      }
    },
    [hydrate, updateWorkspace],
  );

  const leavePod = useCallback(
    async (channel) => {
      const peerId = state?.user?.username || 'local-peer';
      const podName = channel?.podName || channel?.podId;
      if (!channel?.podId) return;
      const prompt =
        channel.podId === GOLD_STAR_CLUB_POD_ID
          ? `Permanently leave ${podName}? Gold Star Club membership is irrevocable.`
          : `Leave pod "${podName}"? This exits the pod and removes its channels.`;
      if (!window.confirm(prompt)) return;
      try {
        await pods.leave(channel.podId, peerId);
        await hydrate();
        updateWorkspace((previous) => {
          const tabs = previous.tabs.filter((tab) => {
            if (tab.type !== 'pod') return true;
            const { podId } = decodePodTarget(tab.target);
            return podId !== channel.podId;
          });
          return tabs.length === previous.tabs.length
            ? previous
            : {
                ...previous,
                activeTabId: tabs.find((tab) => tab.id === previous.activeTabId)
                  ? previous.activeTabId
                  : tabs[0]?.id ?? null,
                tabs,
              };
        });
      } catch (error) {
        console.error('Failed to leave pod:', error);
      }
    },
    [hydrate, state?.user?.username, updateWorkspace],
  );

  const startDirectMessage = useCallback(() => {
    const trimmed = dmDraft.trim();
    if (!trimmed) return;
    openTab('chat', trimmed);
    setDmDraft('');
    setDmAddOpen(false);
  }, [dmDraft, openTab]);

  const joinRoomByName = useCallback(async (roomName) => {
    const trimmed = roomName.trim();
    if (!trimmed) return false;
    setRoomJoinError('');
    try {
      await rooms.join({ roomName: trimmed });
      await hydrate();
      openTab('room', trimmed);
      return true;
    } catch (error) {
      console.error('Failed to join room:', error);
      const detail = error?.response?.data;
      const message = typeof detail === 'string'
        ? detail
        : detail?.detail || detail?.message || error?.message || 'Room join failed. Try again shortly.';
      setRoomJoinError(message);
      return false;
    }
  }, [hydrate, openTab]);

  const joinRoomFromPicker = useCallback(async (roomName) => {
    const joined = await joinRoomByName(roomName);
    if (!joined) return;
    setRoomDraft('');
    setRoomAddOpen(false);
  }, [joinRoomByName]);

  const createPodFromInput = useCallback(async () => {
    const name = podDraft.trim();
    if (!name) return;
    try {
      const created = await pods.create({
        channels: [
          {
            channelId: 'general',
            kind: 'General',
            name: 'General',
          },
        ],
        description: null,
        externalBindings: [],
        name,
        tags: [],
        visibility: 'Unlisted',
      }, currentUser || 'local-peer');
      const channel = {
        channelId: 'general',
        channelName: 'General',
        podId: created.podId,
        podName: created.name || name,
        target: encodePodTarget(created.podId, 'general'),
      };
      await hydrate();
      openTab('pod', channel.target, channelLabel(channel));
      setPodDraft('');
      setPodAddOpen(false);
    } catch (error) {
      console.error('Failed to create pod room:', error);
    }
  }, [currentUser, hydrate, openTab, podDraft]);

  const saveDiscoveredPod = useCallback(async (pod) => {
    const podId = pod.podId || pod.PodId;
    const name = pod.name || pod.Name || podId;
    if (!podId) return;
    try {
      const saved = await pods.create({
        channels: [
          {
            channelId: 'general',
            kind: 'General',
            name: 'General',
          },
        ],
        externalBindings: [],
        focusContentId: pod.focusContentId || pod.FocusContentId || null,
        name,
        podId,
        tags: asArray(pod.tags || pod.Tags),
        visibility: pod.visibility || pod.Visibility || 'Unlisted',
      }, currentUser || 'local-peer');
      const detail = await pods.get(saved.podId).catch(() => saved);
      const firstChannel = asArray(detail.channels)[0] || { channelId: 'general', name: 'General' };
      const channel = {
        channelId: firstChannel.channelId,
        channelName: firstChannel.name,
        podId: saved.podId,
        podName: saved.name || name,
        target: encodePodTarget(saved.podId, firstChannel.channelId),
      };
      await hydrate();
      openTab('pod', channel.target, channelLabel(channel));
    } catch (error) {
      console.error('Failed to save discovered pod:', error);
    }
  }, [currentUser, hydrate, openTab]);

  useEffect(() => {
    const handleKeyDown = (event) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setQsOpen((open) => !open);
      } else if (event.key === 'Escape' && qsOpen) {
        setQsOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [qsOpen]);

  const quickSwitcherItems = useMemo(() => {
    const items = [];
    for (const conversation of conversations) {
      items.push({
        accent: 'slsk',
        id: `chat:${conversation.username}`,
        label: conversation.username,
        prefix: '@',
        sublabel: 'Soulseek DM',
        target: conversation.username,
        type: 'chat',
      });
    }
    for (const roomName of joinedRooms) {
      items.push({
        accent: 'slsk',
        id: `room:${roomName}`,
        label: roomName,
        prefix: '#',
        sublabel: 'Soulseek room',
        target: roomName,
        type: 'room',
      });
    }
    for (const channel of visiblePodChannels) {
      const label = channelLabel(channel);
      items.push({
        accent: 'mesh',
        id: `pod:${channel.target}`,
        label,
        prefix: '&',
        sublabel: 'Mesh channel',
        target: channel.target,
        type: 'pod',
        tabLabel: label,
      });
    }
    return items;
  }, [conversations, joinedRooms, visiblePodChannels]);

  const activeTab = workspace.tabs.find((tab) => tab.id === workspace.activeTabId) ?? null;

  useEffect(() => {
    if (workspace.tabs.length > 0) return;
    if (initialKind === 'chat' && conversations[0]?.username) {
      openTab('chat', conversations[0].username);
    } else if (initialKind === 'room' && joinedRooms[0]) {
      openTab('room', joinedRooms[0]);
    } else if (initialKind === 'pod' && visiblePodChannels[0]) {
      openTab('pod', visiblePodChannels[0].target, channelLabel(visiblePodChannels[0]));
    }
  }, [conversations, initialKind, joinedRooms, openTab, visiblePodChannels, workspace.tabs.length]);

  useEffect(() => {
    if (initialKind !== 'pod' || !params?.podId || visiblePodChannels.length === 0) {
      return;
    }
    const target =
      visiblePodChannels.find(
        (channel) =>
          channel.podId === params.podId &&
          (!params.channelId || channel.channelId === params.channelId),
      ) ?? visiblePodChannels.find((channel) => channel.podId === params.podId);
    if (target) {
      openTab('pod', target.target, channelLabel(target));
    }
  }, [initialKind, openTab, params?.channelId, params?.podId, visiblePodChannels]);

  const showSoulseek = networkFilter !== 'mesh';
  const showMesh = networkFilter !== 'soulseek';

  const activePodChannel = useMemo(
    () =>
      activeTab?.type === 'pod'
        ? podChannels.find((channel) => channel.target === activeTab.target) ?? {
            ...decodePodTarget(activeTab.target),
            podName: activeTab.label,
          }
        : null,
    [activeTab?.label, activeTab?.target, activeTab?.type, podChannels],
  );

  const adapter = useMemo(() => {
    if (!activeTab) return null;
    if (activeTab.type === 'chat') {
      return createChatAdapter({ currentUser, username: activeTab.target });
    }
    if (activeTab.type === 'room') {
      return createRoomAdapter({ currentUser, roomName: activeTab.target });
    }
    if (activeTab.type === 'pod' && activePodChannel) {
      return createPodAdapter({ channel: activePodChannel, currentUser });
    }
    return null;
  }, [activePodChannel, activeTab?.target, activeTab?.type, currentUser]);

  useEffect(() => {
    if (!adapter || typeof adapter.members !== 'function') {
      setAdapterMembers((previous) => (previous.length === 0 ? previous : []));
      return undefined;
    }
    let cancelled = false;
    const applyMembers = (members) => {
      const next = Array.isArray(members) ? members : [];
      setAdapterMembers((previous) =>
        memberListSignature(previous) === memberListSignature(next) ? previous : next,
      );
    };
    const refresh = async () => {
      try {
        const members = await adapter.members();
        if (!cancelled) applyMembers(members);
      } catch {
        if (!cancelled) applyMembers([]);
      }
    };
    refresh();
    const interval = window.setInterval(refresh, 5_000);
    return () => {
      cancelled = true;
      window.clearInterval(interval);
    };
  }, [adapter]);

  const handleSenderClick = useCallback((username, event) => {
    if (!username) return;
    const x = event?.clientX ?? window.innerWidth / 2;
    const y = event?.clientY ?? window.innerHeight / 2;
    setUserPopover({ username, x, y });
  }, []);

  const closeUserPopover = useCallback(() => setUserPopover(null), []);

  useEffect(() => {
    setComposerDraft('');
  }, [activeTab?.id]);

  const handleCopyMessage = useCallback(async (message) => {
    if (!message?.body) return;
    try {
      await navigator.clipboard.writeText(message.body);
    } catch (error) {
      console.error('Copy to clipboard failed:', error);
    }
  }, []);

  const handleQuoteMessage = useCallback((message) => {
    if (!message) return;
    const quote = `> ${message.sender}: ${message.body}\n`;
    setComposerDraft((previous) => `${quote}${previous}`);
    window.setTimeout(() => {
      composerInputRef.current?.focus();
    }, 0);
  }, []);

  const userPopoverActions = useMemo(
    () => ({
      browse: (username) => {
        navigate(`/browse?user=${encodeURIComponent(username)}`, {
          state: { user: username },
        });
        closeUserPopover();
      },
      message: (username) => {
        openTab('chat', username);
        closeUserPopover();
      },
      profile: (username) => {
        navigate(`/users?user=${encodeURIComponent(username)}`, {
          state: { user: username },
        });
        closeUserPopover();
      },
    }),
    [closeUserPopover, navigate, openTab],
  );

  const handleComposerCommand = useCallback(
    ({ argv, name }) => {
      if (name === 'close') {
        if (activeTab) closeTab(activeTab.id);
        return true;
      }
      if (name === 'part' || name === 'leave') {
        if (!activeTab) return true;
        if (activeTab.type === 'room') {
          leaveRoom(activeTab.target);
          return true;
        }
        if (activeTab.type === 'pod' && activePodChannel) {
          leavePod(activePodChannel);
          return true;
        }
        closeTab(activeTab.id);
        return true;
      }
      if (name === 'zoom') {
        const target = (argv[0] || '').toLowerCase();
        if (ZOOM_LEVELS.includes(target)) setZoom(target);
        return true;
      }
      if (name === 'msg' && argv.length >= 1) {
        openTab('chat', argv[0]);
        return true;
      }
      if (name === 'join' && argv.length >= 1) {
        const roomName = argv.join(' ').trim();
        rooms.join({ roomName }).then(() => {
          openTab('room', roomName);
          hydrate();
        });
        return true;
      }
      if (name === 'reload') {
        setStreamReloadToken((value) => value + 1);
        return true;
      }
      if (name === 'switch' || name === 'k') {
        setQsOpen(true);
        return true;
      }
      if (name === 'help' || name === '?') {
        setHelpOpen(true);
        return true;
      }
      return false;
    },
    [activePodChannel, activeTab, closeTab, hydrate, leavePod, leaveRoom, openTab, setZoom],
  );

  const showMembers = activeTab && activeTab.type !== 'chat' && memberRailOpen;
  const joinableRooms = availableRooms.filter(
    (roomName) => !joinedRooms.includes(roomName),
  );
  const joinableDiscoveredPods = discoveredPods.filter((pod) => {
    const podId = pod.podId || pod.PodId;
    return podId && !podChannels.some((channel) => channel.podId === podId);
  });

  const gridStyle = {
    '--msgv2-member-width': showMembers ? `${workspace.paneSettings.memberWidth}px` : '0px',
    '--msgv2-tree-width': `${workspace.paneSettings.treeWidth}px`,
  };

  const totalUnread = conversations.reduce(
    (sum, c) => sum + (c.hasUnAcknowledgedMessages ? c.unAcknowledgedMessageCount || 1 : 0),
    0,
  );

  return (
    <div
      className="msgv2"
      data-zoom={workspace.zoom}
      onWheelCapture={handleWheelZoom}
      style={gridStyle}
    >
      <aside className="msgv2-rail">
        <button
          aria-label="All networks"
          className={`msgv2-rail-pill ${networkFilter === 'all' ? 'is-active' : ''}`}
          onClick={() => setNetworkFilter('all')}
          title="All networks"
          type="button"
        >
          ⌂
        </button>
        <button
          aria-label="Soulseek only"
          className={`msgv2-rail-pill msgv2-rail-slsk ${networkFilter === 'soulseek' ? 'is-active' : ''}`}
          data-accent="slsk"
          onClick={() => setNetworkFilter('soulseek')}
          title="Soulseek only"
          type="button"
        >
          S
          {totalUnread > 0 && <span className="msgv2-rail-badge">{formatUnread(totalUnread)}</span>}
        </button>
        <button
          aria-label="Mesh only"
          className={`msgv2-rail-pill msgv2-rail-mesh ${networkFilter === 'mesh' ? 'is-active' : ''}`}
          data-accent="mesh"
          onClick={() => setNetworkFilter('mesh')}
          title="Mesh only"
          type="button"
        >
          M
        </button>
        <div className="msgv2-rail-spacer" />
        <button
          aria-label="Refresh"
          className="msgv2-rail-pill msgv2-rail-icon"
          onClick={() => {
            hydrate();
            if (roomAddOpen) loadAvailableRooms();
          }}
          title="Refresh"
          type="button"
        >
          ↻
        </button>
      </aside>

      <nav
        aria-label="Conversations"
        className="msgv2-tree"
      >
        <div className="msgv2-tree-header">
          <span className="msgv2-tree-title">Channels</span>
          <span className="msgv2-tree-meta">{NETWORKS.includes(networkFilter) && networkFilter !== 'all' ? networkFilter : ''}</span>
        </div>

        {showSoulseek && (
          <TreeSection
            accent="slsk"
            addLabel="Start a direct message"
            addPanel={
              <InlineAddForm
                buttonLabel="DM"
                onCancel={() => {
                  setDmAddOpen(false);
                  setDmDraft('');
                }}
                onChange={setDmDraft}
                onSubmit={startDirectMessage}
                placeholder="username"
                value={dmDraft}
              />
            }
            collapsed={workspace.collapsedSections.soulseekDirect}
            count={conversations.length}
            onAddToggle={() => setDmAddOpen((open) => !open)}
            onToggle={() => toggleSection('soulseekDirect')}
            showAdd={dmAddOpen}
            title="Soulseek · DMs"
          >
            {conversations.length === 0 ? (
              <EmptyHint>No saved direct messages.</EmptyHint>
            ) : (
              conversations.map((c) => {
                const unread = c.hasUnAcknowledgedMessages
                  ? c.unAcknowledgedMessageCount || 1
                  : 0;
                const isActive =
                  activeTab?.type === 'chat' && activeTab.target === c.username;
                return (
                  <TreeRow
                    accent="slsk"
                    actionLabel={`Delete saved thread with ${c.username}`}
                    isActive={isActive}
                    key={c.username}
                    onAction={() => deleteConversation(c.username)}
                    onActivate={() => openTab('chat', c.username)}
                    prefix="@"
                    target={c.username}
                    unread={unread}
                  />
                );
              })
            )}
          </TreeSection>
        )}

        {showSoulseek && (
          <TreeSection
            accent="slsk"
            addLabel="Join or create a room"
            addPanel={
              <RoomJoinSearch
                availableRooms={joinableRooms}
                joinedRooms={joinedRooms}
                error={roomJoinError}
                onCancel={() => {
                  setRoomAddOpen(false);
                  setRoomDraft('');
                  setRoomJoinError('');
                }}
                onChange={(nextValue) => {
                  setRoomDraft(nextValue);
                  if (roomJoinError) setRoomJoinError('');
                }}
                onJoinRoom={joinRoomFromPicker}
                value={roomDraft}
              />
            }
            collapsed={workspace.collapsedSections.soulseekRooms}
            count={joinedRooms.length + joinableRooms.length}
            onAddToggle={() => setRoomAddOpen((open) => !open)}
            onToggle={() => toggleSection('soulseekRooms')}
            showAdd={roomAddOpen}
            title="Soulseek · Rooms"
          >
            {joinedRooms.length === 0 && joinableRooms.length === 0 ? (
              <EmptyHint>No rooms reported by the Soulseek server.</EmptyHint>
            ) : (
              <>
              {joinedRooms.length === 0 ? (
                <EmptyHint>Search above to join or create a room.</EmptyHint>
              ) : joinedRooms.map((roomName) => {
                const isActive =
                  activeTab?.type === 'room' && activeTab.target === roomName;
                return (
                  <TreeRow
                    accent="slsk"
                    actionLabel={`Leave room ${roomName}`}
                    isActive={isActive}
                    key={roomName}
                    onAction={() => leaveRoom(roomName)}
                    onActivate={() => openTab('room', roomName)}
                    prefix="#"
                    target={roomName}
                  />
                );
              })}
              </>
            )}
          </TreeSection>
        )}

        {showMesh && (
          <TreeSection
            accent="mesh"
            addLabel="Create a pod room"
            addPanel={
              <InlineAddForm
                buttonLabel="Create"
                onCancel={() => {
                  setPodAddOpen(false);
                  setPodDraft('');
                }}
                onChange={setPodDraft}
                onSubmit={createPodFromInput}
                placeholder="pod room name"
                value={podDraft}
              />
            }
            collapsed={workspace.collapsedSections.meshPods}
            count={visiblePodChannels.length + joinableDiscoveredPods.length}
            onAddToggle={() => setPodAddOpen((open) => !open)}
            onToggle={() => toggleSection('meshPods')}
            showAdd={podAddOpen}
            title="Mesh · Pod channels"
          >
            {visiblePodChannels.length === 0 && joinableDiscoveredPods.length === 0 ? (
              <EmptyHint>No pod rooms or discovered pods yet.</EmptyHint>
            ) : (
              <>
              {visiblePodChannels.length === 0 ? (
                <EmptyHint>No joined pod channels.</EmptyHint>
              ) : visiblePodChannels.map((channel) => {
                const label = channelLabel(channel);
                const isActive =
                  activeTab?.type === 'pod' && activeTab.target === channel.target;
                return (
                  <TreeRow
                    accent="mesh"
                    actionLabel={`Leave pod ${channel.podName || channel.podId}`}
                    isActive={isActive}
                    key={channel.target}
                    onAction={() => leavePod(channel)}
                    onActivate={() => openTab('pod', channel.target, label)}
                    prefix="&"
                    target={label}
                  />
                );
              })}
              {joinableDiscoveredPods.length > 0 && (
                <TreeSubhead>Discovered pods</TreeSubhead>
              )}
              {joinableDiscoveredPods.slice(0, 50).map((pod) => {
                const podId = pod.podId || pod.PodId;
                const name = pod.name || pod.Name || podId;
                return (
                  <TreeRow
                    accent="mesh"
                    key={`discovered-${podId}`}
                    onActivate={() => saveDiscoveredPod(pod)}
                    prefix="+"
                    target={name}
                  />
                );
              })}
              </>
            )}
          </TreeSection>
        )}
      </nav>

      <div
        aria-label="Resize channel tree"
        aria-orientation="vertical"
        className={`msgv2-handle msgv2-handle-tree ${treeResize.dragging ? 'is-dragging' : ''}`}
        onKeyDown={treeResize.onKeyDown}
        onPointerDown={treeResize.onPointerDown}
        role="separator"
        tabIndex={0}
      />

      <main className="msgv2-view">
        <header className="msgv2-tabs">
          <div className="msgv2-tabs-strip">
            {workspace.tabs.length === 0 ? (
              <span className="msgv2-tabs-empty">No tabs open</span>
            ) : (
              workspace.tabs.map((tab) => {
                const isActive = tab.id === workspace.activeTabId;
                return (
                  <button
                    aria-current={isActive ? 'page' : undefined}
                    className={`msgv2-tab ${isActive ? 'is-active' : ''}`}
                    data-accent={tabAccent(tab)}
                    key={tab.id}
                    onAuxClick={(event) => {
                      if (event.button === 1) {
                        event.preventDefault();
                        closeTab(tab.id);
                      }
                    }}
                    onClick={() => activateTab(tab.id)}
                    title={tabSubtitle(tab)}
                    type="button"
                  >
                    <span className="msgv2-tab-label">{tabLabel(tab)}</span>
                    <span
                      aria-label={`Close ${tabLabel(tab)}`}
                      className="msgv2-tab-close"
                      onClick={(event) => {
                        event.stopPropagation();
                        closeTab(tab.id);
                      }}
                      role="button"
                      tabIndex={-1}
                    >
                      ×
                    </span>
                  </button>
                );
              })
            )}
          </div>
          <div className="msgv2-tabs-actions">
            {activeTab && activeTab.type !== 'chat' && (
              <button
                aria-pressed={memberRailOpen}
                className={`msgv2-icon-button ${memberRailOpen ? 'is-on' : ''}`}
                onClick={() => setMemberRailOpen((open) => !open)}
                title={memberRailOpen ? 'Hide members' : 'Show members'}
                type="button"
              >
                ☰
              </button>
            )}
            <DensityToggle
              onAdjust={adjustZoom}
              onChange={setZoom}
              value={workspace.zoom}
            />
          </div>
        </header>

        <section className="msgv2-stage">
          {activeTab ? (
            <MessageStream
              adapter={adapter}
              emptyHint={`No messages yet in ${tabLabel(activeTab)}.`}
              key={`${activeTab.id}#${streamReloadToken}`}
              onCopy={handleCopyMessage}
              onQuote={handleQuoteMessage}
              onSenderClick={handleSenderClick}
            />
          ) : (
            <div className="msgv2-empty">
              <div className="msgv2-empty-glyph">⌬</div>
              <div className="msgv2-empty-title">No conversation open</div>
              <div className="msgv2-empty-hint">
                Pick something from the channel list, or press <kbd>Ctrl</kbd>+<kbd>K</kbd>.
              </div>
            </div>
          )}
        </section>

        <Composer
          adapter={adapter}
          commands={COMPOSER_COMMANDS}
          inputRef={composerInputRef}
          label={activeTab ? `Message ${tabLabel(activeTab)}` : 'Message composer'}
          onChange={setComposerDraft}
          onCommand={handleComposerCommand}
          placeholder={
            activeTab
              ? `Message ${tabLabel(activeTab)} — type / for commands, /help for the full list`
              : undefined
          }
          value={composerDraft}
        />
      </main>

      {showMembers && (
        <div
          aria-label="Resize member rail"
          aria-orientation="vertical"
          className={`msgv2-handle msgv2-handle-member ${memberResize.dragging ? 'is-dragging' : ''}`}
          onKeyDown={memberResize.onKeyDown}
          onPointerDown={memberResize.onPointerDown}
          role="separator"
          tabIndex={0}
        />
      )}

      {showMembers && (
        <MemberRail
          members={adapterMembers}
          onSelect={handleSenderClick}
        />
      )}

      <QuickSwitcher
        items={quickSwitcherItems}
        onClose={() => setQsOpen(false)}
        onPick={(item) => {
          openTab(item.type, item.target, item.tabLabel);
          setQsOpen(false);
        }}
        open={qsOpen}
      />

      <CommandHelp
        commands={COMPOSER_COMMANDS}
        onClose={() => setHelpOpen(false)}
        open={helpOpen}
      />

      <UserPopover
        anchor={userPopover ? { x: userPopover.x, y: userPopover.y } : null}
        onBrowse={userPopoverActions.browse}
        onClose={closeUserPopover}
        onMessage={userPopoverActions.message}
        onProfile={userPopoverActions.profile}
        open={Boolean(userPopover)}
        username={userPopover?.username}
      />
    </div>
  );
};

const memberKey = (member) =>
  member.peerId || member.username || member.PeerId || member.name || '';

const memberDisplay = (member) =>
  member.peerId || member.username || member.PeerId || member.name || 'unknown';

const memberRole = (member) =>
  member.role || member.Role || (member.isOperator ? 'operator' : null);

const memberListSignature = (members) =>
  members
    .map((member) => `${memberKey(member)}\u0001${memberDisplay(member)}\u0001${memberRole(member) || ''}`)
    .sort()
    .join('\u0002');

const MemberRail = React.memo(({ members, onSelect }) => (
  <aside
    aria-label="Members"
    className="msgv2-members"
  >
    <div className="msgv2-members-header">
      Members <span className="msgv2-members-count">{members.length}</span>
    </div>
    {members.length === 0 ? (
      <div className="msgv2-members-hint">No members reported yet.</div>
    ) : (
      <ul className="msgv2-members-list">
        {members.map((member) => {
          const display = memberDisplay(member);
          const role = memberRole(member);
          return (
            <li
              className="msgv2-members-item"
              key={memberKey(member) || display}
            >
              <button
                className="msgv2-members-name"
                onClick={(event) => onSelect?.(display, event)}
                title={display}
                type="button"
              >
                {display}
              </button>
              {role && <span className="msgv2-members-role">{role}</span>}
            </li>
          );
        })}
      </ul>
    )}
  </aside>
));

const TreeSection = ({
  accent,
  addLabel,
  addPanel,
  collapsed,
  count,
  onAddToggle,
  onToggle,
  showAdd,
  title,
  children,
}) => (
  <section
    className={`msgv2-tree-section ${collapsed ? 'is-collapsed' : ''}`}
    data-accent={accent}
  >
    <div className="msgv2-tree-section-head-row">
      <button
        aria-expanded={!collapsed}
        className="msgv2-tree-section-head"
        onClick={onToggle}
        type="button"
      >
        <span className="msgv2-tree-caret">{collapsed ? '▸' : '▾'}</span>
        <span className="msgv2-tree-section-title">{title}</span>
        <span className="msgv2-tree-section-count">{count}</span>
      </button>
      {onAddToggle && (
        <button
          aria-label={addLabel}
          aria-pressed={showAdd}
          className={`msgv2-tree-section-add ${showAdd ? 'is-on' : ''}`}
          onClick={onAddToggle}
          title={addLabel}
          type="button"
        >
          {showAdd ? '×' : '+'}
        </button>
      )}
    </div>
    {!collapsed && showAdd && addPanel && (
      <div className="msgv2-tree-add-panel">{addPanel}</div>
    )}
    {!collapsed && <div className="msgv2-tree-section-body">{children}</div>}
  </section>
);

const TreeRow = ({
  accent,
  actionLabel,
  isActive,
  onAction,
  onActivate,
  prefix,
  target,
  unread,
}) => (
  <div
    className={`msgv2-tree-row-wrap ${isActive ? 'is-active' : ''}`}
    data-accent={accent}
  >
    <button
      className={`msgv2-tree-row ${isActive ? 'is-active' : ''}`}
      data-accent={accent}
      onClick={onActivate}
      title={`${prefix}${target}`}
      type="button"
    >
      <span className="msgv2-tree-row-prefix">{prefix}</span>
      <span className="msgv2-tree-row-name">{target}</span>
      {unread > 0 && (
        <span className="msgv2-tree-row-unread">{formatUnread(unread)}</span>
      )}
    </button>
    {onAction && (
      <button
        aria-label={actionLabel}
        className="msgv2-tree-row-action"
        onClick={(event) => {
          event.stopPropagation();
          onAction();
        }}
        tabIndex={-1}
        title={actionLabel}
        type="button"
      >
        ×
      </button>
    )}
  </div>
);

const EmptyHint = ({ children }) => (
  <div className="msgv2-tree-empty">{children}</div>
);

const TreeSubhead = ({ children }) => (
  <div className="msgv2-tree-subhead">{children}</div>
);

const RoomJoinSearch = ({
  availableRooms,
  error,
  joinedRooms,
  onCancel,
  onChange,
  onJoinRoom,
  value,
}) => {
  const inputRef = useRef(null);
  const query = value.trim();
  const normalizedQuery = query.toLocaleLowerCase();
  const joinedLookup = useMemo(
    () => new Set(joinedRooms.map((roomName) => roomName.toLocaleLowerCase())),
    [joinedRooms],
  );
  const matches = useMemo(() => {
    if (!normalizedQuery) return [];
    const exact = [];
    const startsWith = [];
    const contains = [];
    const seen = new Set();
    const addMatch = (roomName, status) => {
      const normalizedRoom = roomName.toLocaleLowerCase();
      if (seen.has(normalizedRoom)) return;
      seen.add(normalizedRoom);
      const match = { roomName, status };
      if (normalizedRoom === normalizedQuery) exact.push(match);
      else if (normalizedRoom.startsWith(normalizedQuery)) startsWith.push(match);
      else if (normalizedRoom.includes(normalizedQuery)) contains.push(match);
    };
    joinedRooms.forEach((roomName) => addMatch(roomName, 'joined'));
    availableRooms.forEach((roomName) => addMatch(roomName, 'available'));
    return [...exact, ...startsWith, ...contains].slice(0, 16);
  }, [availableRooms, joinedRooms, normalizedQuery]);
  const exactAvailable = matches.some(
    (match) =>
      match.status === 'available' &&
      match.roomName.toLocaleLowerCase() === normalizedQuery,
  );
  const exactJoined = normalizedQuery && joinedLookup.has(normalizedQuery);
  const canSubmit = query.length > 0;
  const submitLabel = exactJoined ? 'Open' : exactAvailable ? 'Join' : 'Join/Create';

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  const submit = useCallback(() => {
    if (!canSubmit) return;
    onJoinRoom(query);
  }, [canSubmit, onJoinRoom, query]);

  return (
    <div className="msgv2-room-search">
      <div className="msgv2-tree-add">
        <input
          aria-label="Search or create Soulseek room"
          className="msgv2-tree-add-input"
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              submit();
            } else if (event.key === 'Escape') {
              event.preventDefault();
              onCancel();
            }
          }}
          placeholder="search or create room"
          ref={inputRef}
          type="text"
          value={value}
        />
        <button
          aria-label="Join/Create room"
          className="msgv2-tree-add-go"
          disabled={!canSubmit}
          onClick={submit}
          title="Join a matching Soulseek room, or create it if it does not exist"
          type="button"
        >
          {submitLabel}
        </button>
      </div>
      {!query ? (
        <div className="msgv2-room-search-hint">
          {availableRooms.length > 0
            ? `${availableRooms.length.toLocaleString()} available. Type to filter.`
            : 'Room directory unavailable or empty. Type a room name to join/create.'}
        </div>
      ) : (
        <>
          {matches.length > 0 && (
            <div
              aria-label="Matching Soulseek rooms"
              className="msgv2-room-search-results"
            >
              {matches.map(({ roomName, status }) => (
                <button
                  aria-label={`${status === 'joined' ? 'Open' : 'Join'} ${roomName}`}
                  className="msgv2-room-search-result"
                  key={roomName}
                  onClick={() => onJoinRoom(roomName)}
                  title={`${status === 'joined' ? 'Open' : 'Join'} ${roomName}`}
                  type="button"
                >
                  <span className="msgv2-tree-row-prefix">#</span>
                  <span className="msgv2-tree-row-name">{roomName}</span>
                  <span className="msgv2-room-search-result-meta">
                    {status === 'joined' ? 'Open' : 'Join'}
                  </span>
                </button>
              ))}
            </div>
          )}
          {!exactAvailable && !exactJoined && (
            <button
              className="msgv2-room-search-create"
              onClick={submit}
              title={`Join or create ${query}`}
              type="button"
            >
              Join/Create #{query}
            </button>
          )}
          {exactJoined && (
            <div className="msgv2-room-search-hint">Already joined. Press Enter to open.</div>
          )}
        </>
      )}
      {error && <div className="msgv2-room-search-error">{error}</div>}
    </div>
  );
};

const InlineAddForm = ({
  buttonLabel,
  onCancel,
  onChange,
  onSubmit,
  placeholder,
  value,
}) => {
  const inputRef = useRef(null);
  useEffect(() => {
    inputRef.current?.focus();
  }, []);
  return (
    <div className="msgv2-tree-add">
      <input
        className="msgv2-tree-add-input"
        onChange={(event) => onChange(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter') {
            event.preventDefault();
            onSubmit();
          } else if (event.key === 'Escape') {
            event.preventDefault();
            onCancel();
          }
        }}
        placeholder={placeholder}
        ref={inputRef}
        type="text"
        value={value}
      />
      <button
        className="msgv2-tree-add-go"
        disabled={value.trim().length === 0}
        onClick={onSubmit}
        type="button"
      >
        {buttonLabel}
      </button>
    </div>
  );
};

const DensityToggle = ({ onAdjust, onChange, value }) => (
  <div
    aria-label="Messages UI size"
    className="msgv2-density"
  >
    <button
      aria-label="Make Messages UI smaller"
      className="msgv2-density-step"
      disabled={zoomIndex(value) === 0}
      onClick={() => onAdjust(-1)}
      title="Make the entire Messages UI smaller"
      type="button"
    >
      −
    </button>
    <div
      aria-label="Messages UI size presets"
      className="msgv2-density-presets"
      role="radiogroup"
    >
      {ZOOM_LEVELS.map((level) => (
        <button
          aria-checked={value === level}
          className={`msgv2-density-pip ${value === level ? 'is-active' : ''}`}
          key={level}
          onClick={() => onChange(level)}
          role="radio"
          title={`Messages UI size ${level.toUpperCase()}`}
          type="button"
        >
          {level.toUpperCase()}
        </button>
      ))}
    </div>
    <button
      aria-label="Make Messages UI larger"
      className="msgv2-density-step"
      disabled={zoomIndex(value) === ZOOM_LEVELS.length - 1}
      onClick={() => onAdjust(1)}
      title="Make the entire Messages UI larger"
      type="button"
    >
      +
    </button>
  </div>
);

const useDragResize = ({ direction, onChange, range, value }) => {
  const [dragging, setDragging] = useState(false);
  const startRef = useRef(null);

  const handleMove = useCallback(
    (event) => {
      if (!startRef.current) return;
      const dx = event.clientX - startRef.current.x;
      const delta = direction === 'right' ? dx : -dx;
      const next = Math.min(range.max, Math.max(range.min, startRef.current.value + delta));
      onChange(next);
    },
    [direction, onChange, range.max, range.min],
  );

  const stopDrag = useCallback(() => {
    setDragging(false);
    startRef.current = null;
    window.removeEventListener('pointermove', handleMove);
    window.removeEventListener('pointerup', stopDrag);
    document.body.classList.remove('msgv2-resizing');
  }, [handleMove]);

  const onPointerDown = useCallback(
    (event) => {
      event.preventDefault();
      startRef.current = { value, x: event.clientX };
      setDragging(true);
      document.body.classList.add('msgv2-resizing');
      window.addEventListener('pointermove', handleMove);
      window.addEventListener('pointerup', stopDrag);
    },
    [handleMove, stopDrag, value],
  );

  const onKeyDown = useCallback(
    (event) => {
      if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
      event.preventDefault();
      const sign = event.key === 'ArrowRight' ? 1 : -1;
      const delta = direction === 'right' ? sign * 16 : -sign * 16;
      const next = Math.min(range.max, Math.max(range.min, value + delta));
      onChange(next);
    },
    [direction, onChange, range.max, range.min, value],
  );

  useEffect(
    () => () => {
      window.removeEventListener('pointermove', handleMove);
      window.removeEventListener('pointerup', stopDrag);
      document.body.classList.remove('msgv2-resizing');
    },
    [handleMove, stopDrag],
  );

  return { dragging, onKeyDown, onPointerDown };
};

export default MessagingV2;
