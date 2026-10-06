import React from 'react';
import { Button, Popup } from 'semantic-ui-react';

const getText = (children) => {
  if (typeof children === 'string') {
    return children;
  }

  if (Array.isArray(children)) {
    return children.filter((child) => typeof child === 'string').join(' ').trim();
  }

  return '';
};

const TooltipButton = ({
  'aria-label': ariaLabel,
  children,
  content: buttonContent,
  popupPosition = 'top center',
  title,
  tooltip,
  ...props
}) => {
  const inferredLabel = ariaLabel || title || getText(children) || getText(buttonContent) ||
    (typeof tooltip === 'string' ? tooltip : undefined);
  const button = (
    <Button
      aria-label={ariaLabel || inferredLabel}
      content={buttonContent}
      title={title}
      {...props}
    >
      {children}
    </Button>
  );
  const popupContent = tooltip || title || inferredLabel;

  if (!popupContent) {
    return button;
  }

  return (
    <Popup
      content={popupContent}
      position={popupPosition}
      trigger={props.disabled ? <span>{button}</span> : button}
    />
  );
};

TooltipButton.Group = Button.Group;
TooltipButton.Or = Button.Or;

export default TooltipButton;
