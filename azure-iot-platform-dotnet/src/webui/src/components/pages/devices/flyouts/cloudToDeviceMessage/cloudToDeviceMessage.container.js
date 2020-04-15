// Copyright (c) Microsoft. All rights reserved.

import { connect } from 'react-redux';
import { withNamespaces } from 'react-i18next';
import { CloudToDeviceMessage } from './cloudToDeviceMessage';
import { redux as deviceRedux } from 'store/reducers/devicesReducer';
import { epics as appEpics, getTheme } from 'store/reducers/appReducer';


const mapStateToProps = state => ({
    theme: getTheme(state)
  });
// Wrap the dispatch method
const mapDispatchToProps = dispatch => ({
});

export const CloudToDeviceMessageContainer = withNamespaces()(connect(mapStateToProps, mapDispatchToProps)(CloudToDeviceMessage));
