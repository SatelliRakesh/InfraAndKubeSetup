// Copyright (c) Microsoft. All rights reserved.

import { Observable } from 'rxjs';

import Config from 'app.config';
import { stringify } from 'query-string';
import { HttpClient } from 'utilities/httpClient';
import { v4 as uuidv4 } from 'uuid'
import {
  toUserTenantModel
} from './models';

const ENDPOINT = Config.serviceUrls.identityGateway;

/** Contains methods for calling the Identity Gateway service */
export class IdentityGatewayService {

  /** Returns a list of users */
  static getUsers() {
    
    return HttpClient.get(`${ENDPOINT}tenants/users`)
      .map((res = {Models: []}) => res.Models)
      .map(toUserTenantModel);
  }

  /** Delete a User */
  static deleteUser(id) {
    
    return HttpClient.delete(`${ENDPOINT}tenants/${id}`)
      .map(t => id);
  }
  /** Invite a new User */
  static invite(email, role) {

    return HttpClient.post(`${ENDPOINT}tenants/invite`, {
      "email_address": email,
      "role": role
    })
    .map(t=> toUserTenantModel([t]));
  }

  /** Add a new Service Principal */
  static addSP(appid, role) {

    return HttpClient.post(`${ENDPOINT}tenants/${appid}`, {
      "PartitionKey": "", // placeholder not used
      "RowKey": "",  // placeholder not used
      "Roles": `['${role}']`,
      "Type": "Client Credentials",
      "Name": appid
    })
    .map(t=> toUserTenantModel([t]));
  }
}
