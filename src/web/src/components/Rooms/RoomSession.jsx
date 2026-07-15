import * as rooms from '../../lib/rooms';
import React, { Component, createRef } from 'react';
import UserCard from '../Shared/UserCard';
import {
  Button,
  Card,
  Dimmer,
  Icon,
  Input,
  List,
  Loader,
  Portal,
  Popup,
  Ref,
  Segment,
} from 'semantic-ui-react';

const initialState = {
  contextMenu: {
    message: null,
    open: false,
    x: 0,
    y: 0,
  },
  loading: false,
  message: '',
  room: {
    messages: [],
    users: [],
  },
};

const MESSAGE_POLL_INTERVAL_MS = 2_000;
const USER_POLL_INTERVAL_MS = 10_000;

const sameMessages = (previous, next) =>
  previous.length === next.length &&
  previous.every(
    (message, index) =>
      message.timestamp === next[index]?.timestamp &&
      message.username === next[index]?.username &&
      message.message === next[index]?.message &&
      message.self === next[index]?.self,
  );

const sameUsers = (previous, next) =>
  previous.length === next.length &&
  previous.every(
    (user, index) =>
      user.username === next[index]?.username &&
      user.status === next[index]?.status,
  );

class RoomSession extends Component {
  constructor(props) {
    super(props);

    this.state = initialState;
    this.listRef = createRef();
    this.messageRef = undefined;
    this.messageInterval = undefined;
    this.messageRequest = null;
    this.mounted = false;
    this.userInterval = undefined;
    this.userRequest = null;
  }

  componentDidMount() {
    this.mounted = true;
    document.addEventListener('visibilitychange', this.handleVisibilityChange);
    if (this.props.active !== false && !document.hidden) {
      this.startPolling();
    }

    document.addEventListener('click', this.handleCloseContextMenu);
  }

  componentWillUnmount() {
    this.mounted = false;
    this.stopPolling();
    document.removeEventListener('visibilitychange', this.handleVisibilityChange);
    document.removeEventListener('click', this.handleCloseContextMenu);
  }

  componentDidUpdate(previousProps) {
    if (previousProps.roomName !== this.props.roomName) {
      this.stopPolling();
      this.setState(initialState, () => {
        if (this.props.active !== false && !document.hidden) {
          this.startPolling();
          this.focusInput();
        }
      });
    }

    if (previousProps.active === false && this.props.active !== false) {
      this.startPolling();
      this.focusInput();
    }

    if (previousProps.active !== false && this.props.active === false) {
      this.stopPolling();
    }
  }

  startPolling = () => {
    if (this.props.active === false || document.hidden) {
      return;
    }

    if (!this.messageInterval) {
      this.fetchMessages();
      this.messageInterval = window.setInterval(
        this.fetchMessages,
        MESSAGE_POLL_INTERVAL_MS,
      );
    }

    if (!this.userInterval) {
      this.fetchUsers();
      this.userInterval = window.setInterval(
        this.fetchUsers,
        USER_POLL_INTERVAL_MS,
      );
    }
  };

  stopPolling = () => {
    if (this.messageInterval) {
      window.clearInterval(this.messageInterval);
      this.messageInterval = undefined;
    }

    if (this.userInterval) {
      window.clearInterval(this.userInterval);
      this.userInterval = undefined;
    }
  };

  handleVisibilityChange = () => {
    if (document.hidden) {
      this.stopPolling();
      return;
    }

    if (this.props.active !== false) {
      this.startPolling();
    }
  };

  fetchMessages = () => {
    const { roomName } = this.props;

    if (!roomName || roomName.length === 0) return Promise.resolve();
    if (this.messageRequest?.roomName === roomName) {
      return this.messageRequest.promise;
    }

    const request = (async () => {
      try {
        const messages = await rooms.getMessages({ roomName });
        if (
          !this.mounted ||
          document.hidden ||
          this.props.active === false ||
          this.props.roomName !== roomName
        ) {
          return;
        }

        const next = Array.isArray(messages) ? messages : [];
        this.setState((previous) =>
          sameMessages(previous.room.messages, next)
            ? null
            : { room: { ...previous.room, messages: next } },
        );
      } catch (error) {
        console.error('Failed to fetch room messages:', error);
      }
    })();
    const tracked = {
      promise: request.finally(() => {
        if (this.messageRequest === tracked) {
          this.messageRequest = null;
        }
      }),
      roomName,
    };
    this.messageRequest = tracked;
    return tracked.promise;
  };

  fetchUsers = () => {
    const { roomName } = this.props;

    if (!roomName || roomName.length === 0) return Promise.resolve();
    if (this.userRequest?.roomName === roomName) {
      return this.userRequest.promise;
    }

    const request = (async () => {
      try {
        const users = await rooms.getUsers({ roomName });
        if (
          !this.mounted ||
          document.hidden ||
          this.props.active === false ||
          this.props.roomName !== roomName
        ) {
          return;
        }

        const next = Array.isArray(users) ? users : [];
        this.setState((previous) =>
          sameUsers(previous.room.users, next)
            ? null
            : { room: { ...previous.room, users: next } },
        );
      } catch (error) {
        console.error('Failed to fetch room users:', error);
      }
    })();
    const tracked = {
      promise: request.finally(() => {
        if (this.userRequest === tracked) {
          this.userRequest = null;
        }
      }),
      roomName,
    };
    this.userRequest = tracked;
    return tracked.promise;
  };

  validInput = () =>
    (this.props.roomName || '').length > 0 &&
    (this.state.message || '').trim().length > 0;

  focusInput = () => {
    if (this.messageRef && this.messageRef.current) {
      this.messageRef.current.focus();
    }
  };

  formatTimestamp = (timestamp) => {
    const date = new Date(timestamp);
    const dtfUS = new Intl.DateTimeFormat('en', {
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      month: 'numeric',
    });

    return dtfUS.format(date);
  };

  sendMessage = async () => {
    const { roomName } = this.props;
    const { message } = this.state;

    if (!this.validInput()) {
      return;
    }

    try {
      await rooms.sendMessage({ message: message.trim(), roomName });
      this.setState({ message: '' });
    } catch (error) {
      console.error('Failed to send message:', error);
    }
  };

  handleContextMenu = (clickEvent, message) => {
    clickEvent.preventDefault();
    this.setState({
      contextMenu: {
        message,
        open: true,
        x: clickEvent.pageX,
        y: clickEvent.pageY,
      },
    });
  };

  handleCloseContextMenu = () => {
    this.setState((previousState) => ({
      contextMenu: {
        ...previousState.contextMenu,
        open: false,
      },
    }));
  };

  handleReply = () => {
    const selectedMessage = this.state.contextMenu.message;

    if (this.messageRef && this.messageRef.current && selectedMessage) {
      this.setState({
        message: `[${selectedMessage.username}] ${selectedMessage.message} --> `,
      });
      this.focusInput();
    }
  };

  handleUserProfile = () => {
    if (this.props.onUserProfile) {
      this.props.onUserProfile(this.state.contextMenu.message.username);
    }
  };

  handleBrowseShares = () => {
    if (this.props.onBrowseShares) {
      this.props.onBrowseShares(this.state.contextMenu.message.username);
    }
  };

  renderContextMenu() {
    const { contextMenu } = this.state;
    return (
      <Portal open={contextMenu.open}>
        <div
          className="ui vertical buttons popup-menu"
          style={{
            left: contextMenu.x,
            maxHeight: `calc(100vh - ${contextMenu.y}px)`,
            top: contextMenu.y,
          }}
        >
          <Popup
            content="Quote this room message in the composer."
            trigger={
              <Button
                className="ui compact button popup-option"
                onClick={this.handleReply}
                title="Reply to this message"
              >
                Reply
              </Button>
            }
          />
          <Popup
            content="Open this user's profile page."
            trigger={
              <Button
                className="ui compact button popup-option"
                onClick={this.handleUserProfile}
                title="Open user profile"
              >
                User Profile
              </Button>
            }
          />
          <Popup
            content="Browse this user's shared files."
            trigger={
              <Button
                className="ui compact button popup-option"
                onClick={this.handleBrowseShares}
                title="Browse user shares"
              >
                Browse Shares
              </Button>
            }
          />
        </div>
      </Portal>
    );
  }

  render() {
    const { onLeaveRoom, roomName } = this.props;

    const { contextMenu, loading, message, room } = this.state;

    if (!roomName || roomName.length === 0) {
      return (
        <div className="room-session-empty">
          <Segment placeholder>
            <Icon
              name="comments"
              size="big"
            />
            <p>Select a room to start chatting</p>
          </Segment>
        </div>
      );
    }

    if (this.props.active === false) {
      return (
        <div className="room-session room-session-inactive">
          <Card className="room-active-card">
            <Card.Content>
              <Card.Header>
                <Icon
                  color="grey"
                  name="comments"
                />
                {roomName}
                <Icon
                  className="close-button"
                  color="red"
                  link
                  name="close"
                  onClick={() => onLeaveRoom && onLeaveRoom(roomName)}
                />
              </Card.Header>
            </Card.Content>
          </Card>
        </div>
      );
    }

    return (
      <div className="room-session">
        <Card
          className="room-active-card"
          raised
        >
          <Card.Content onClick={() => this.focusInput()}>
            <Card.Header>
              <Icon
                color="green"
                name="circle"
              />
              {roomName}
              <Icon
                className="close-button"
                color="red"
                link
                name="close"
                onClick={() => onLeaveRoom && onLeaveRoom(roomName)}
              />
            </Card.Header>
            <div className="room">
              {loading ? (
                <Dimmer
                  active
                  inverted
                >
                  <Loader inverted />
                </Dimmer>
              ) : (
                <>
                  <Segment.Group>
                    <Segment className="room-history">
                      <Ref innerRef={this.listRef}>
                        <List>
                          {room.messages.map((message) => (
                            <div
                              key={`${message.timestamp}+${message.message}`}
                              onContextMenu={(clickEvent) =>
                                this.handleContextMenu(clickEvent, message)
                              }
                            >
                              <List.Content
                                className={`room-message ${message.self ? 'room-message-self' : ''}`}
                              >
                                <span className="room-message-time">
                                  {this.formatTimestamp(message.timestamp)}
                                </span>
                                <span className="room-message-name">
                                  {message.username}:{' '}
                                </span>
                                <span className="room-message-message">
                                  {message.message}
                                </span>
                              </List.Content>
                            </div>
                          ))}
                          <List.Content id="room-history-scroll-anchor" />
                        </List>
                      </Ref>
                    </Segment>
                    <Segment className="room-input">
                      <Input
                        action={{
                          'aria-label': 'Send room message',
                          className: 'room-message-button',
                          disabled: !this.validInput(),
                          icon: (
                            <Icon
                              color="green"
                              name="send"
                            />
                          ),
                          onClick: this.sendMessage,
                          title: 'Send message to this room',
                        }}
                        fluid
                        input={
                          <input
                            autoComplete="off"
                            data-lpignore="true"
                            id="room-message-input"
                            type="text"
                          />
                        }
                        onChange={(event, { value }) =>
                          this.setState({ message: value })
                        }
                        onKeyUp={(event) =>
                          event.key === 'Enter' ? this.sendMessage() : ''
                        }
                        ref={(input) =>
                          (this.messageRef = input && input.inputRef)
                        }
                        transparent
                        value={message}
                      />
                    </Segment>
                  </Segment.Group>
                  <Segment className="room-users">
                    <div className="room-users-header">
                      <Icon name="users" />
                      Users ({room.users.length})
                    </div>
                    <List
                      divided
                      relaxed
                    >
                      {room.users.map((user) => (
                        <List.Item key={user.username}>
                          <List.Content>
                            <List.Header><UserCard username={user.username}>{user.username}</UserCard></List.Header>
                            <List.Description>
                              {user.status === 1 ? 'Away' : 'Online'}
                            </List.Description>
                          </List.Content>
                        </List.Item>
                      ))}
                    </List>
                  </Segment>
                </>
              )}
            </div>
          </Card.Content>
        </Card>
        {this.renderContextMenu()}
      </div>
    );
  }
}

export default RoomSession;
