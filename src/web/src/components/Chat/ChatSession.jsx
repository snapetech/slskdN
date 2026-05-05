import './Chat.css';
import * as chat from '../../lib/chat';
import PlaceholderSegment from '../Shared/PlaceholderSegment';
import UserCard from '../Shared/UserCard';
import React, { Component, createRef } from 'react';
import {
  Card,
  Dimmer,
  Icon,
  Input,
  List,
  Loader,
  Ref,
  Segment,
} from 'semantic-ui-react';

class ChatSession extends Component {
  constructor(props) {
    super(props);

    this.state = {
      conversation: null,
      interval: undefined,
      loading: false,
      message: '',
    };

    this.listRef = createRef();
    this.messageRef = undefined;
  }

  componentDidMount() {
    if (this.props.active === false) {
      return;
    }

    this.startPolling();
  }

  startPolling = () => {
    if (this.state.interval) {
      return;
    }

    const interval = window.setInterval(this.fetchConversation, 5_000);
    this.setState({ interval }, () => {
      this.fetchConversation();
    });
  };

  stopPolling = (updateState = true) => {
    if (!this.state.interval) {
      return;
    }

    clearInterval(this.state.interval);
    if (updateState) {
      this.setState({ interval: undefined });
    }
  };

  componentDidUpdate(previousProps) {
    // If username changed, fetch new conversation
    if (
      previousProps.username !== this.props.username ||
      (!previousProps.active && this.props.active)
    ) {
      const usernameChanged = previousProps.username !== this.props.username;
      this.setState(usernameChanged ? { message: '' } : {}, () => {
        this.fetchConversation();
        if (this.props.active !== false) {
          this.focusInput();
        }
      });
    }

    if (previousProps.active === false && this.props.active !== false) {
      this.startPolling();
    }

    if (previousProps.active !== false && this.props.active === false) {
      this.stopPolling();
    }
  }

  componentWillUnmount() {
    this.stopPolling(false);
  }

  fetchConversation = async () => {
    const { username } = this.props;
    if (!username) {
      this.setState({ conversation: null, loading: false });
      return;
    }

    this.setState({ loading: true });

    try {
      const conversation = await chat.get({ username });

      // Acknowledge unread messages only when the user is looking at this tab.
      if (this.props.active !== false && conversation?.hasUnAcknowledgedMessages) {
        await chat.acknowledge({ username });
      }

      this.setState({ conversation, loading: false }, () => {
        // Scroll to bottom
        try {
          if (this.listRef.current?.lastChild) {
            this.listRef.current.lastChild.scrollIntoView();
          }
        } catch {
          // no-op
        }
      });
    } catch (error) {
      console.error('Failed to fetch conversation:', error);
      this.setState({ conversation: null, loading: false });
    }
  };

  sendMessage = async (message) => {
    const { username } = this.props;
    if (!username || !message) return;

    await chat.send({ message, username });
    this.setState({ message: '' });

    // Refresh to show new message
    await this.fetchConversation();
  };

  sendReply = async () => {
    const { message } = this.state;
    if (!message || !message.trim()) return;

    await this.sendMessage(message.trim());
  };

  validInput = () => {
    const { username } = this.props;
    const { message } = this.state;
    return username && message && message.trim().length > 0;
  };

  focusInput = () => {
    if (this.messageRef?.current) {
      this.messageRef.current.focus();
    }
  };

  handleFocusInput = () => {
    this.focusInput();
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

  deleteConversation = async () => {
    const { onDelete, username } = this.props;
    if (!username) return;

    await chat.remove({ username });
    if (onDelete) {
      onDelete(username);
    }
  };

  render() {
    const { user, username } = this.props;
    const { conversation, loading, message } = this.state;
    const messages = conversation?.messages || [];

    if (!username) {
      return (
        <PlaceholderSegment
          caption="Select a conversation or start a new chat"
          icon="comment"
        />
      );
    }

    return (
      <Card
        className="chat-active-card"
        raised
      >
        <Card.Content onClick={this.handleFocusInput}>
          <Card.Header>
            <Icon
              color="green"
              name="circle"
            />
            <UserCard username={username}>{username}</UserCard>
            <Icon
              className="close-button"
              color="red"
              link
              name="close"
              onClick={this.deleteConversation}
            />
          </Card.Header>
          <div className="chat">
            {loading ? (
              <Dimmer
                active
                inverted
              >
                <Loader inverted />
              </Dimmer>
            ) : (
              <Segment.Group>
                <Segment className="chat-history">
                  <Ref innerRef={this.listRef}>
                    <List>
                      {messages.map((message) => (
                        <List.Content
                          className={`chat-message ${message.direction === 'Out' ? 'chat-message-self' : ''}`}
                          key={`${message.timestamp}+${message.message}`}
                        >
                          <span className="chat-message-time">
                            {this.formatTimestamp(message.timestamp)}
                          </span>
                          <span className="chat-message-name">
                            {message.direction === 'Out'
                              ? user?.username || 'You'
                              : message.username}
                            :{' '}
                          </span>
                          <span className="chat-message-message">
                            {message.message}
                          </span>
                        </List.Content>
                      ))}
                      <List.Content id="chat-history-scroll-anchor" />
                    </List>
                  </Ref>
                </Segment>
                <Segment className="chat-input">
                  <Input
                    action={{
                      className: 'chat-message-button',
                      disabled: !this.validInput(),
                      icon: (
                        <Icon
                          color="green"
                          name="send"
                        />
                      ),
                      onClick: this.sendReply,
                    }}
                    fluid
                    input={
                      <input
                        autoComplete="off"
                        data-lpignore="true"
                        id="chat-message-input"
                        type="text"
                      />
                    }
                    onChange={(event, { value }) =>
                      this.setState({ message: value })
                    }
                    onKeyUp={(event) =>
                      event.key === 'Enter' ? this.sendReply() : ''
                    }
                    ref={(input) => (this.messageRef = input && input.inputRef)}
                    transparent
                    value={message}
                  />
                </Segment>
              </Segment.Group>
            )}
          </div>
        </Card.Content>
      </Card>
    );
  }
}

export default ChatSession;
