"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classnames = require("classnames/bind");
const styled_components_1 = require("styled-components");
const Attributes_1 = require("../../Attributes");
const css = classnames.bind(require('./Button.module.scss'));
const StyledPrimaryButton = styled_components_1.default(Attributes_1.Elements.button) `
    &&&&& {
        color: ${(props) => props.theme.colorTextBtnPrimaryRest};
        background-color: ${(props) => props.theme.colorBgBtnPrimaryRest};
        &:hover { 
            background-color: ${(props) => props.theme.colorBgBtnPrimaryHover};
        }
        &:disabled {
            color: ${(props) => props.theme.colorTextBtnPrimaryDisabled};
            background-color: ${(props) => props.theme.colorBgBtnPrimaryDisabled};
        }
    }
`;
/**
 * Button showing Information, Warning, or Error with text, icon, and optional close button
 *
 * @param props Control properties (defined in `ButtonProps` interface)
 */
exports.Button = (props) => {
    const icon = props.icon ? React.createElement(Attributes_1.Elements.span, { className: css(`icon icon-${props.icon}`), attr: props.attr.icon }) : '';
    const ButtonProxy = props.primary ? StyledPrimaryButton : Attributes_1.Elements.button;
    return (React.createElement(ButtonProxy, { type: props.type, title: props.title, className: css('btn', {
            'btn-primary': props.primary
        }, props.className), onClick: props.onClick, disabled: props.disabled, attr: props.attr.container },
        icon,
        props.children));
};
exports.Button.defaultProps = {
    onClick: undefined,
    type: 'button',
    attr: {
        container: {},
        icon: {}
    }
};
exports.default = exports.Button;
