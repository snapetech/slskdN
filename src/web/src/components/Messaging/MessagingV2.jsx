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
import MessageStream from './MessageStream';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useNavigate, useParams } from 'react-router-dom';

const NETWORKS = ['all', 'soulseek', 'mesh'];

const sectionAccent = {
  mesh: 'mesh',
  soulseek: 'slsk',
};

const tabAccent = (tab) => (tab.type === 'pod' ? 'mesh' : 'slsk');

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
  }, []);

  const [conversations, setConversations] = useState([]);
  const [joinedRooms, setJoinedRooms] = useState([]);
  const [podChannels, setPodChannels] = useState([]);
  const [networkFilter, setNetworkFilter] = useState('all');
  const [memberRailOpen, setMemberRailOpen] = useState(true);
  const [adapterMembers, setAdapterMembers] = useState([]);

  const hydrate = useCallback(async () => {
    const [serverConversations, serverJoinedRooms, serverPods] = await Promise.all([
      chat.getAll(),
      rooms.getJoined(),
      pods.list().catch(() => []),
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

  const activePodChannel =
    activeTab?.type === 'pod'
      ? podChannels.find((channel) => channel.target === activeTab.target) ?? {
          ...decodePodTarget(activeTab.target),
          podName: activeTab.label,
        }
      : null;

  const currentUser = state?.user?.username;

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
  }, [activeTab, activePodChannel, currentUser]);

  useEffect(() => {
    setAdapterMembers([]);
    if (!adapter || typeof adapter.members !== 'function') return undefined;
    let cancelled = false;
    const refresh = async () => {
      try {
        const members = await adapter.members();
        if (!cancelled) setAdapterMembers(Array.isArray(members) ? members : []);
      } catch {
        if (!cancelled) setAdapterMembers([]);
      }
    };
    refresh();
    const interval = window.setInterval(refresh, 5_000);
    return () => {
      cancelled = true;
      window.clearInterval(interval);
    };
  }, [adapter]);

  const handleSenderClick = useCallback(
    (username) => {
      if (!username) return;
      navigate(`/users?user=${encodeURIComponent(username)}`, {
        state: { user: username },
      });
    },
    [navigate],
  );

  const handleComposerCommand = useCallback(
    ({ argv, name }) => {
      if (name === 'close' || name === 'part' || name === 'leave') {
        if (activeTab) closeTab(activeTab.id);
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
      return false;
    },
    [activeTab, closeTab, hydrate, openTab, setZoom],
  );

  const showMembers = activeTab && activeTab.type !== 'chat' && memberRailOpen;

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
          onClick={() => hydrate()}
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
            collapsed={workspace.collapsedSections.soulseekDirect}
            count={conversations.length}
            onToggle={() => toggleSection('soulseekDirect')}
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
                    isActive={isActive}
                    key={c.username}
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
            collapsed={workspace.collapsedSections.soulseekRooms}
            count={joinedRooms.length}
            onToggle={() => toggleSection('soulseekRooms')}
            title="Soulseek · Rooms"
          >
            {joinedRooms.length === 0 ? (
              <EmptyHint>No joined rooms.</EmptyHint>
            ) : (
              joinedRooms.map((roomName) => {
                const isActive =
                  activeTab?.type === 'room' && activeTab.target === roomName;
                return (
                  <TreeRow
                    accent="slsk"
                    isActive={isActive}
                    key={roomName}
                    onActivate={() => openTab('room', roomName)}
                    prefix="#"
                    target={roomName}
                  />
                );
              })
            )}
          </TreeSection>
        )}

        {showMesh && (
          <TreeSection
            accent="mesh"
            collapsed={workspace.collapsedSections.meshPods}
            count={visiblePodChannels.length}
            onToggle={() => toggleSection('meshPods')}
            title="Mesh · Pod channels"
          >
            {visiblePodChannels.length === 0 ? (
              <EmptyHint>No pod channels yet.</EmptyHint>
            ) : (
              visiblePodChannels.map((channel) => {
                const label = channelLabel(channel);
                const isActive =
                  activeTab?.type === 'pod' && activeTab.target === channel.target;
                return (
                  <TreeRow
                    accent="mesh"
                    isActive={isActive}
                    key={channel.target}
                    onActivate={() => openTab('pod', channel.target, label)}
                    prefix="&"
                    target={label}
                  />
                );
              })
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
              onSenderClick={handleSenderClick}
            />
          ) : (
            <div className="msgv2-empty">
              <div className="msgv2-empty-glyph">⌬</div>
              <div className="msgv2-empty-title">No conversation open</div>
              <div className="msgv2-empty-hint">
                Pick something from the channel list to start.
              </div>
            </div>
          )}
        </section>

        <Composer
          adapter={adapter}
          label={activeTab ? `Message ${tabLabel(activeTab)}` : 'Message composer'}
          onCommand={handleComposerCommand}
          placeholder={
            activeTab
              ? `Message ${tabLabel(activeTab)} — /me, /msg, /join, /close`
              : undefined
          }
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
    </div>
  );
};

const memberKey = (member) =>
  member.peerId || member.username || member.PeerId || member.name || '';

const memberDisplay = (member) =>
  member.peerId || member.username || member.PeerId || member.name || 'unknown';

const memberRole = (member) =>
  member.role || member.Role || (member.isOperator ? 'operator' : null);

const MemberRail = ({ members, onSelect }) => (
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
                onClick={() => onSelect?.(display)}
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
);

const TreeSection = ({ accent, collapsed, count, onToggle, title, children }) => (
  <section
    className={`msgv2-tree-section ${collapsed ? 'is-collapsed' : ''}`}
    data-accent={accent}
  >
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
    {!collapsed && <div className="msgv2-tree-section-body">{children}</div>}
  </section>
);

const TreeRow = ({ accent, isActive, onActivate, prefix, target, unread }) => (
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
);

const EmptyHint = ({ children }) => (
  <div className="msgv2-tree-empty">{children}</div>
);

const DensityToggle = ({ onChange, value }) => (
  <div
    aria-label="Density"
    className="msgv2-density"
    role="radiogroup"
  >
    {ZOOM_LEVELS.map((level) => (
      <button
        aria-checked={value === level}
        className={`msgv2-density-pip ${value === level ? 'is-active' : ''}`}
        key={level}
        onClick={() => onChange(level)}
        role="radio"
        title={`Density ${level.toUpperCase()}`}
        type="button"
      >
        {level.toUpperCase()}
      </button>
    ))}
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
