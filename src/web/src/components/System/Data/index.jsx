import { clearCompleted } from '../../../lib/transfers';
import React, { useState } from 'react';
import { toast } from 'react-toastify';
import { Button, Divider, Header, Icon, Message, Popup } from 'semantic-ui-react';

const clear = async ({ direction, setError, setLoading }) => {
  setLoading(true);
  setError('');

  try {
    await clearCompleted({ direction });
    toast.success(`Completed ${direction}s cleared!`);
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.message ||
      `Unable to clear completed ${direction}s.`;
    setError(message);
  } finally {
    setLoading(false);
  }
};

const Data = () => {
  const [up, setUp] = useState(false);
  const [down, setDown] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const [downloadError, setDownloadError] = useState('');

  return (
    <div className="transfer-data">
      <Header
        as="h2"
        className="transfer-header"
      >
        Transfer Data
      </Header>
      <Divider />
      <p>
        The Uploads and Downloads pages can become unresponsive if too many
        transfers are displayed. Remove completed transfer records to keep those
        pages responsive; downloaded files on disk are not removed.
      </p>
      <div className="transfer-data-actions">
        <Popup
          content="Remove completed upload records from transfer history to keep the Uploads page responsive."
          on={['hover', 'focus']}
          position="top center"
          trigger={(
            <Button
              disabled={up}
              loading={up}
              negative
              onClick={() => clear({
                direction: 'upload',
                setError: setUploadError,
                setLoading: setUp,
              })}
            >
              <Icon name="trash alternate" />
              Clear All Completed Uploads
            </Button>
          )}
        />
        <Popup
          content="Remove completed download records from transfer history. Files already saved on disk are not deleted."
          on={['hover', 'focus']}
          position="top center"
          trigger={(
            <Button
              disabled={down}
              loading={down}
              negative
              onClick={() => clear({
                direction: 'download',
                setError: setDownloadError,
                setLoading: setDown,
              })}
            >
              <Icon name="trash alternate" />
              Clear All Completed Downloads
            </Button>
          )}
        />
      </div>
      {(uploadError || downloadError) && (
        <div aria-live="polite" className="transfer-data-errors">
          {uploadError && (
            <Message
              content={`Uploads: ${uploadError}`}
              negative
              role="alert"
            />
          )}
          {downloadError && (
            <Message
              content={`Downloads: ${downloadError}`}
              negative
              role="alert"
            />
          )}
        </div>
      )}
    </div>
  );
};

export default Data;
