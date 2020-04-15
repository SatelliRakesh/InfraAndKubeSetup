"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classNames = require("classnames/bind");
const Attributes_1 = require("../../Attributes");
const css = classNames.bind(require('./RadioInput.module.scss'));
/**
 * Low level radio button control
 *
 * (Use the `RadioField` control instead when making a form with standard styling)
 *
 * @param props Control properties (defined in `RadioInputProps` interface)
 */
exports.RadioInput = (props) => {
    const classes = { 'disabled': props.disabled, 'selected': props.checked };
    const containerClass = css('radio-container', {
        'columns': props.columns,
        'hidden': props.hidden
    }, props.className);
    const id = `${props.name}_${props.value}`;
    const onClick = (event) => {
        event.stopPropagation();
        props.onChange(props.value);
    };
    return (React.createElement(Attributes_1.Elements.div, { className: containerClass, attr: props.attr.container },
        React.createElement(Attributes_1.Elements.label, { className: css('radio-label', classes), htmlFor: id, onClick: stopPropagation, attr: props.attr.label },
            React.createElement("div", { className: css('radio-wrapper') },
                React.createElement(Attributes_1.Elements.input, { id: id, type: 'radio', value: props.value, name: props.name, disabled: props.disabled, hidden: props.hidden, checked: props.checked, onChange: onClick, autoFocus: props.autoFocus, required: props.required, attr: props.attr.input }),
                React.createElement(Attributes_1.Elements.span, { className: css('radio-button', classes), attr: props.attr.radio }),
                React.createElement(Attributes_1.Elements.span, { className: css('radio-fill', classes), attr: props.attr.fill })),
            React.createElement(Attributes_1.Elements.span, { className: css('radio-text'), attr: props.attr.text }, props.label))));
};
function stopPropagation(e) {
    // HACK! If we don't add this click event handler to the label, React never
    // fires the input onChange handler in IoT Central.
    e.stopPropagation();
}
exports.RadioInput.defaultProps = {
    name: undefined,
    value: undefined,
    label: undefined,
    onChange: undefined,
    columns: false,
    hidden: false,
    attr: {
        container: {},
        label: {},
        input: {},
        radio: {},
        text: {},
        fill: {},
        border: {},
    }
};
exports.default = exports.RadioInput;
