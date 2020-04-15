"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classNames = require("classnames/bind");
const Attributes_1 = require("../../Attributes");
const SolidBackground_1 = require("./SolidBackground");
const css = classNames.bind(require('./GalleryCard.module.scss'));
/**
 * Gallery Card control
 *
 * You should usually mark this control as `fixed` because the container
 * element gets its width from its content like the background and children
 *
 * @param props Control properties (Defined in `GalleryCardProps` interface)
 */
exports.GalleryCard = (props) => {
    let classNames = css({
        'card': true,
        'fixed': !!props.fixed,
        'fullbg': !props.children
    }, props.className || '');
    let contentClassName = css({
        'card-content': true,
    }, props.contentClassName);
    let outputProps = {
        className: classNames
    };
    if (props.dataTestHook) {
        outputProps['data-test-hook'] = props.dataTestHook;
    }
    const banner = props.banner ? (React.createElement(exports.Banner, { attr: { container: props.attr.banner } }, props.banner)) : null;
    const content = props.children ? (React.createElement(Attributes_1.Elements.div, { className: contentClassName, attr: props.attr.content }, props.children)) : null;
    return (React.createElement(Attributes_1.Elements.div, Object.assign({}, outputProps, { attr: props.attr.container }),
        props.background,
        content,
        banner));
};
exports.GalleryCard.defaultProps = {
    fixed: true,
    background: React.createElement(SolidBackground_1.SolidBackground, null),
    attr: {
        container: {},
        content: {},
        banner: {},
    }
};
/** TODO: Remove this Banner control. GalleryCard banner is now a string */
exports.Banner = (props) => {
    let cls = css({
        'banner': true,
    }, props.className);
    return (React.createElement(Attributes_1.Elements.div, { className: cls, attr: props.attr.container }, props.children));
};
exports.Banner.defaultProps = {
    attr: {
        container: {}
    }
};
exports.default = exports.GalleryCard;
