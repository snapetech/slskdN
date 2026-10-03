import { getCurrentDebugView } from '../../../lib/options';
import { CodeEditor, PlaceholderSegment, Switch } from '../../Shared';
import React, { useEffect, useRef, useState } from 'react';
import { Button, Icon, Message, Modal, Popup } from 'semantic-ui-react';

const getErrorText = (error) => {
  const data = error?.response?.data;
  if (typeof data === 'string') return data;
  if (data && typeof data === 'object') {
    return data.detail || data.message || data.error || data.title || JSON.stringify(data);
  }
  return error?.message || 'Could not load the debug view.';
};

const DebugModal = ({ onClose, open, theme }) => {
  const [loading, setLoading] = useState(true);
  const [debugView, setDebugView] = useState();
  const [error, setError] = useState();
  const requestId = useRef(0);

  const get = async () => {
    const nextRequestId = ++requestId.current;
    setLoading(true);
    setError(undefined);

    try {
      const result = await getCurrentDebugView();
      if (nextRequestId === requestId.current) setDebugView(result);
    } catch (caught) {
      if (nextRequestId === requestId.current) setError(getErrorText(caught));
    } finally {
      if (nextRequestId === requestId.current) setLoading(false);
    }
  };

  useEffect(() => {
    if (open) {
      get();
    }
    return () => {
      requestId.current += 1;
    };
  }, [open]);

  return (
    <Modal
      onClose={onClose}
      open={open}
      size="large"
    >
      <Modal.Header>
        <Icon name="bug" />
        Options (Debug View)
      </Modal.Header>
      <Modal.Content
        className="debug-view-content"
        scrolling
      >
        <Switch loading={loading && <PlaceholderSegment loading />}>
          {error ? (
            <Message negative>
              <Message.Content>{error}</Message.Content>
              <Popup
                content="Retry loading the current debug view."
                trigger={(
                  <Button onClick={get} primary>
                    Retry Debug View
                  </Button>
                )}
              />
            </Message>
          ) : (
            <CodeEditor
              basicSetup={false}
              editable={false}
              style={{ minHeight: 500 }}
              theme={theme}
              value={debugView}
            />
          )}
        </Switch>
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Close the debug view and return to Options."
          trigger={<Button onClick={onClose}>Close</Button>}
        />
      </Modal.Actions>
    </Modal>
  );
};

export default DebugModal;
