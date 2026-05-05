import React from 'react';
import { Button as SemanticButton, Popup } from 'semantic-ui-react';

const getButtonText = (children) => {
  if (typeof children === 'string') {
    return children;
  }

  if (Array.isArray(children)) {
    return children.filter((child) => typeof child === 'string').join(' ').trim();
  }

  return '';
};

const Button = ({
  'aria-label': ariaLabel,
  children,
  title,
  tooltip,
  ...props
}) => {
  const label = ariaLabel || title || getButtonText(children) || undefined;
  const button = (
    <SemanticButton
      aria-label={ariaLabel || label}
      title={title}
      {...props}
    >
      {children}
    </SemanticButton>
  );
  const content = tooltip || title || label;

  if (!content) {
    return button;
  }

  return (
    <Popup
      content={content}
      trigger={button}
    />
  );
};

Button.Group = SemanticButton.Group;
Button.Or = SemanticButton.Or;

export default Button;
