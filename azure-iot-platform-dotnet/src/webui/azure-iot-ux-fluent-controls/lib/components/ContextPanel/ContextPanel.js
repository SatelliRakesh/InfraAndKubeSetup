"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classnames = require("classnames/bind");
const ActionTrigger_1 = require("../ActionTrigger");
const Attributes_1 = require("../../Attributes");
const portal_1 = require("./portal");
const cx = classnames.bind(require('./ContextPanel.module.scss'));
function ContextPanel(props) {
    const panel = React.createElement(Panel, Object.assign({}, props));
    if (props.omitPortal) {
        return panel;
    }
    return (React.createElement(portal_1.Portal, null, panel));
}
exports.ContextPanel = ContextPanel;
function Panel({ header, children, footer, onClose, attr }) {
    return (React.createElement(Attributes_1.Elements.div, { role: 'complementary', "aria-labelledby": 'context-panel-title', "aria-describedby": 'context-panel-content', className: cx('panel'), attr: attr && attr.container },
        onClose && React.createElement(ActionTrigger_1.ActionTriggerButton, { icon: 'cancel', className: cx('close-button'), onClick: onClose, attr: attr && attr.closeButton }),
        header && React.createElement(Attributes_1.Elements.div, { id: 'context-panel-title', className: cx('title', 'inline-text-overflow'), attr: attr && attr.header }, header),
        React.createElement(Attributes_1.Elements.div, { id: 'context-panel-content', className: cx('content'), attr: attr && attr.content }, children),
        footer && React.createElement(React.Fragment, null,
            React.createElement("span", { className: cx('separator') }),
            React.createElement(Attributes_1.Elements.div, { className: cx('footer'), attr: attr && attr.footer }, footer))));
}
exports.default = ContextPanel;
