"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const React = require("react");
const classnames = require("classnames/bind");
const styled_components_1 = require("styled-components");
const Masthead_1 = require("../Masthead/Masthead");
const Navigation_1 = require("../Navigation/Navigation");
const root_1 = require("../ContextPanel/root");
var Navigation_2 = require("../Navigation/Navigation");
exports.NavigationItemSeparator = Navigation_2.NavigationItemSeparator;
const css = classnames.bind(require('./Shell.module.scss'));
function Shell({ theme, isRtl, masthead, navigation, children, onClick }) {
    // backward compatibility handle string format theme
    const shellTheme = typeof theme === 'object' ? theme : {
        base: theme
    };
    if (shellTheme.base === undefined) {
        shellTheme.base = 'light';
    }
    return (React.createElement("div", { className: css('theme-' + shellTheme.base) },
        React.createElement(styled_components_1.ThemeProvider, { theme: shellTheme },
            React.createElement("div", { className: css('shell', { rtl: isRtl }), onClick: onClick },
                masthead && React.createElement(Masthead_1.Masthead, Object.assign({ navigation: navigation }, masthead)),
                React.createElement("div", { className: css('nav-and-workspace') },
                    navigation && React.createElement(Navigation_1.Navigation, Object.assign({}, navigation)),
                    React.createElement("div", { className: css('workspace') }, children),
                    React.createElement(root_1.Root, null))))));
}
exports.Shell = Shell;
exports.default = Shell;
