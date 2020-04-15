// Copyright (c) Microsoft. All rights reserved.

import update from 'immutability-helper';
import { camelCaseReshape, reshape, getItems, stringToBoolean, base64toHEX } from 'utilities';
import { INACTIVE_PACKAGE_TAG } from '../configService';

export const toDeviceGroupModel = (deviceGroup = {}) => camelCaseReshape(deviceGroup, {
  'id': 'id',
  'displayName': 'displayName',
  'conditions': 'conditions',
  'eTag': 'eTag'
});

export const toDeviceConditionModel = (condition = {}) => ({
  key: condition.field,
  operator: condition.operator,
  // parse the value as a number or string depending on the value of condition.type and condition.value.
  value: condition.type == 'Number' && !isNaN(condition.value) ? parseFloat(condition.value) : String(condition.value)
});

export const toDeviceGroupsModel = (response = {}) => getItems(response)
  .map(toDeviceGroupModel);

export const toCreateDeviceGroupRequestModel = (params = {}) => ({
  DisplayName: params.displayName,
  Conditions: (params.conditions || []).map(condition => toDeviceConditionModel(condition))
});

export const toUpdateDeviceGroupRequestModel = (params = {}) => ({
  Id: params.id,
  ETag: params.eTag,
  DisplayName: params.displayName,
  Conditions: (params.conditions || []).map(condition => toDeviceConditionModel(condition))
});

export const prepareLogoResponse = ({ xhr, response }) => {
  const returnObj = {};
  const isDefault = xhr.getResponseHeader('IsDefault');
  if (!stringToBoolean(isDefault)) {
    const appName = xhr.getResponseHeader('Name');
    if (appName) {
      returnObj['name'] = appName;
    }
    if (response && response.size) {
      returnObj['logo'] = URL.createObjectURL(response);
    }
  }
  return returnObj;
}

export const toSolutionSettingThemeModel = (response = {}) => camelCaseReshape(response, {
  'description': 'description',
  'name': 'name',
  'diagnosticsOptIn': 'diagnosticsOptIn',
  'azureMapsKey': 'azureMapsKey'
});

export const toSolutionSettingActionModel = (action = {}) => {
  const modelData = camelCaseReshape(action, {
    'type': 'id',
    'settings.isEnabled': 'isEnabled',
    'settings.applicationPermissionsAssigned': 'applicationPermissionsAssigned',
    'settings': 'settings'
  });
  return update(modelData, {
    settings: {
      $unset: ['isEnabled', 'applicationPermissionsAssigned']
    }
  });
}

export const toSolutionSettingActionsModel = (response = {}) => getItems(response)
  .map(toSolutionSettingActionModel);

export const packagesEnum = {
  'edgeManifest': 'EdgeManifest',
  'deviceConfiguration': 'DeviceConfiguration'
}

export const packageTypeOptions = Object.values(packagesEnum);

export const configsEnum = {
  'firmware': 'Firmware',
  'custom': 'Custom'
}

export const configTypeOptions = Object.values(configsEnum);

export const toNewPackageRequestModel = ({
  packageType,
  configType,
  packageFile
}) => {
  const data = new FormData();
  data.append('PackageType', packageType);
  data.append('ConfigType', configType);
  data.append('Package', packageFile);
  return data;
}
export const toNewFirmwareUploadRequestModel = (firmwareFile) => {
  const data = new FormData();
  data.append("uploadedFile", firmwareFile);
  return data;
}
export const toFirmwareModel = (response ={}) => { return {FileUri: response.SoftwarePackageURL || "", CheckSum: base64toHEX(response.CheckSum.SHA1).toLowerCase() || "" }}

export const toPackagesModel = (response = {}) => getItems(response)
  .map(toPackageModel);

export const toPackageModel = (response = {}) => {
  var dataModel = camelCaseReshape(response, {
    'id': 'id',
    'packageType': 'packageType',
    'configType': 'configType',
    'name': 'name',
    'dateCreated': 'dateCreated',
    'content': 'content',
    'tags': 'tags'
  });
  dataModel.active = !(dataModel.tags || []).includes(INACTIVE_PACKAGE_TAG);
  return dataModel;
};

export const toConfigTypesModel = (response = {}) => getItems(response);
