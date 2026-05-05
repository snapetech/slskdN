import React from 'react';
import { Icon, Message } from 'semantic-ui-react';

const PodWorkflowNotice = ({
  children,
  color = 'yellow',
  icon = 'warning sign',
  title,
}) => (
  <Message
    color={color}
    icon
    size="small"
  >
    <Icon name={icon} />
    <Message.Content>
      <Message.Header>{title}</Message.Header>
      {children}
    </Message.Content>
  </Message>
);

export default PodWorkflowNotice;
