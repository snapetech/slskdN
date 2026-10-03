import { getVersion, restart, shutdown } from '../../../lib/application';
import DiagnosticBundleModal from './DiagnosticBundleModal';
import SetupHealthCheckModal from './SetupHealthCheckModal';
import {
  CodeEditor,
  LoaderSegment,
  ShrinkableButton,
  Switch,
} from '../../Shared';
import SystemOverview from './SystemOverview';
import React, { useEffect, useState } from 'react';
import { Button, Divider, Icon, Modal, Popup } from 'semantic-ui-react';
import YAML from 'yaml';

const ConfirmApplicationAction = ({
  actionLabel,
  children,
  disabled,
  icon,
  mediaQuery,
  onConfirm,
  prompt,
  title,
  triggerColor,
  triggerNegative,
}) => {
  const [open, setOpen] = useState(false);

  const confirm = () => {
    setOpen(false);
    onConfirm();
  };

  return (
    <>
      <ShrinkableButton
        color={triggerColor}
        disabled={disabled}
        icon={icon}
        mediaQuery={mediaQuery}
        negative={triggerNegative}
        onClick={() => setOpen(true)}
        tooltip={title}
      >
        {children || actionLabel}
      </ShrinkableButton>
      <Modal
        centered
        onClose={() => setOpen(false)}
        open={open}
        size="mini"
      >
        <Modal.Header>
          <Icon name={icon} />
          Confirm {actionLabel}
        </Modal.Header>
        <Modal.Content>{prompt}</Modal.Content>
        <Modal.Actions>
          <Popup
            content="Close this prompt without changing the application state."
            trigger={(
              <Button onClick={() => setOpen(false)}>
                Cancel
              </Button>
            )}
          />
          <Popup
            content={`${actionLabel} the application.`}
            trigger={(
              <Button negative onClick={confirm}>
                {actionLabel}
              </Button>
            )}
          />
        </Modal.Actions>
      </Modal>
    </>
  );
};

const Info = ({ options, state, theme }) => {
  const [contents, setContents] = useState();

  useEffect(() => {
    let active = true;
    const timeout = window.setTimeout(() => {
      if (!active) return;

      setContents(
        YAML.stringify(state, { simpleKeys: true, sortMapEntries: false }),
      );
    }, 250);

    return () => {
      active = false;
      window.clearTimeout(timeout);
    };
  }, [state]);

  const { pendingRestart } = state;

  return (
    <>
      <SystemOverview state={state} />
      <div className="header-buttons">
        <div style={{ float: 'left' }}>
          <ShrinkableButton
            icon="refresh"
            mediaQuery="(max-width: 686px)"
            onClick={() => getVersion({ forceCheck: true })}
            primary
            tooltip="Check GitHub for the latest slskdN release."
          >
            Check for Updates
          </ShrinkableButton>
          {/* Neutral, not amber — this is an optional external upsell from
              Soulseek itself, not an app action that needs to alarm anyone. */}
          <ShrinkableButton
            disabled={!state?.user?.username}
            icon="star"
            mediaQuery="(max-width: 686px)"
            onClick={() =>
              window.open(
                `http://www.slsknet.org/qtlogin.php?username=${state?.user?.username}`,
                '_blank',
              )
            }
            tooltip="Review Soulseek privilege options for this account."
          >
            Get Privileges
          </ShrinkableButton>
          <DiagnosticBundleModal
            options={options}
            state={state}
          />
          <SetupHealthCheckModal
            options={options}
            state={state}
          />
        </div>
        <ConfirmApplicationAction
          actionLabel="Shut Down"
          icon="shutdown"
          mediaQuery="(max-width: 686px)"
          onConfirm={shutdown}
          prompt="Shutting down stops the application. You'll need to start it manually to use it again."
          title="Shut down the application. Use this when you need to stop the server completely."
        />
        <ConfirmApplicationAction
          actionLabel="Restart"
          icon="redo"
          mediaQuery="(max-width: 686px)"
          onConfirm={restart}
          prompt="Restarting briefly interrupts the web app and Soulseek connections."
          title="Restart slskdN to apply pending changes or recover the application."
          triggerColor={pendingRestart ? 'yellow' : undefined}
          triggerNegative={!pendingRestart}
        />
      </div>
      <Divider />
      <details className="system-state-details">
        <summary>Application state (advanced)</summary>
        <Switch loading={!contents && <LoaderSegment />}>
          <CodeEditor
            basicSetup={false}
            editable={false}
            theme={theme}
            value={contents}
          />
        </Switch>
      </details>
    </>
  );
};

export default Info;
