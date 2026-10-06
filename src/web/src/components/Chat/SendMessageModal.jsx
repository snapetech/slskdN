import './Chat.css';
import React, { useEffect } from 'react';
import { Form, Header, Icon, Input, Modal } from 'semantic-ui-react';
import { TooltipButton } from '../Shared';

const usernameRef = React.createRef();

const SendMessageModal = ({ initiateConversation, ...rest }) => {
  const [open, setOpen] = React.useState(false);
  const [username, setUsername] = React.useState('');
  const [message, setMessage] = React.useState('');

  useEffect(() => {
    if (open) {
      usernameRef.current.focus();
    }
  }, [open]);

  const validInput = () => {
    return username.length > 0 && message.length > 0;
  };

  const sendMessage = async () => {
    if (!validInput()) {
      usernameRef.current.focus();
      return;
    }

    await initiateConversation(username, message);
    setOpen(false);
  };

  return (
    <Modal
      onClose={() => setOpen(false)}
      onOpen={() => setOpen(true)}
      open={open}
      {...rest}
    >
      <Header>
        <Icon name="send" />
        <Modal.Content>Send Private Message</Modal.Content>
      </Header>
      <Modal.Content>
        <Form>
          <Form.Field>
            <Input
              onChange={(_event, data) => setUsername(data.value)}
              placeholder="Username"
              ref={usernameRef}
            />
          </Form.Field>
          <Form.Field>
            <Input
              onChange={(_event, data) => setMessage(data.value)}
              placeholder="Message"
            />
          </Form.Field>
        </Form>
      </Modal.Content>
      <Modal.Actions>
        <TooltipButton
          onClick={() => setOpen(false)}
          tooltip="Close this dialog without sending the private message."
        >
          Cancel
        </TooltipButton>
        <TooltipButton
          disabled={!validInput()}
          onClick={() => sendMessage()}
          positive
          tooltip={
            validInput()
              ? 'Start or open a private conversation and send this message.'
              : 'Enter a username and message before sending.'
          }
        >
          Send
        </TooltipButton>
      </Modal.Actions>
    </Modal>
  );
};

export default SendMessageModal;
