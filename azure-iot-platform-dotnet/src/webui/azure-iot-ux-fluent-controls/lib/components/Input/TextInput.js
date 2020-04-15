"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classNames = require("classnames/bind");
const Attributes_1 = require("../../Attributes");
const Common_1 = require("../../Common");
const css = classNames.bind(require('./TextInput.module.scss'));
exports.prefixClassName = css('prefix-addon');
exports.postfixClassName = css('postfix-addon');
/**
 * Low level text input control
 *
 * (Use the `TextField` control instead when making a form with standard styling)
 */
class TextInput extends React.PureComponent {
    constructor(props) {
        super(props);
        this.onChange = this.onChange.bind(this);
        this.onClear = this.onClear.bind(this);
    }
    onChange(event) {
        const targetValue = event.target.value;
        if (this.props.value !== targetValue) {
            this.props.onChange(targetValue);
        }
        event.stopPropagation();
    }
    onClear() {
        this.props.onChange('');
    }
    render() {
        const containerClassName = css('text-input-container', this.props.className);
        const inputContainerClassName = css('input-container');
        const inputClassName = css({
            'input': true,
            'error': this.props.error,
            'show-cancel': !!this.props.value && this.props.type !== 'number'
        });
        const cancelClassName = css('cancel', 'icon icon-cancelLegacy');
        const clearButton = (this.props.disabled || this.props.readOnly || this.props.type === 'number') ? '' :
            React.createElement(Attributes_1.Elements.button, { type: 'button', className: cancelClassName, onClick: this.onClear, tabIndex: -1, "aria-label": 'Cancel', attr: this.props.attr.clearButton });
        let prefix = null;
        if (this.props.prefix) {
            const className = css('prefix', this.props.prefixClassName);
            prefix = (React.createElement(Attributes_1.Elements.div, { className: className, attr: this.props.attr.prefix }, this.props.prefix));
        }
        let postfix = null;
        if (this.props.postfix) {
            const className = css('postfix', this.props.postfixClassName);
            postfix = (React.createElement(Attributes_1.Elements.div, { className: className, attr: this.props.attr.postfix }, this.props.postfix));
        }
        return (React.createElement(Attributes_1.Elements.div, { className: containerClassName, attr: this.props.attr.container },
            prefix,
            React.createElement(Attributes_1.Elements.div, { className: inputContainerClassName, attr: this.props.attr.inputContainer },
                React.createElement(Attributes_1.Elements.input, { type: this.props.type, name: this.props.name, value: this.props.value == null ? '' : this.props.value, className: inputClassName, onChange: this.onChange, placeholder: this.props.placeholder, required: this.props.required, disabled: this.props.disabled, readOnly: this.props.readOnly, autoFocus: this.props.autoFocus, methodRef: this.props.autoFocus && Common_1.autoFocusRef, attr: this.props.attr.input }),
                clearButton),
            postfix));
    }
}
TextInput.defaultProps = {
    name: undefined,
    value: undefined,
    onChange: undefined,
    type: 'text',
    attr: {
        container: {},
        input: {},
        inputContainer: {},
        prefix: {},
        postfix: {},
        clearButton: {},
    }
};
exports.TextInput = TextInput;
exports.default = TextInput;
