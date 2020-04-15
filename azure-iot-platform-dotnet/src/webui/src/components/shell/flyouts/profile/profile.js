// Copyright (c) Microsoft. All rights reserved.

import React from 'react';
import { Trans } from 'react-i18next';
import Config from 'app.config';
import { permissions } from 'services/models';
import { svgs, getEnumTranslation } from 'utilities';
import {
  Btn,
  BtnToolbar,
  ErrorMsg,
  Hyperlink,
  PropertyGrid as Grid,
  PropertyRow as Row,
  PropertyCell as Cell,
  Protected,
} from 'components/shared';
import Flyout from 'components/shared/flyout';
import {Policies} from 'utilities'

import './profile.scss';

const Section = Flyout.Section;
const jwt_decode = require('jwt-decode');
export const Profile = (props) => {
  const { t, user, logout, switchTenant, createTenant, tenants, fetchTenants, isPending, processTenantDisplayValue, onClose, deleteTenantThenSwitch } = props;

  const roleArray = Array.from(user.roles).map(r => Policies.filter(p=> p.Role == r).concat({DisplayName:"No Roles"})[0].DisplayName);
  const permissionArray = Array.from(user.permissions);
  const currentTenant = user.tenant;

  return (
    <Flyout.Container header={t('profileFlyout.title')} t={t} onClose={onClose}>
      <div className="profile-container">
        {
          !user &&
          <div className="profile-container">
            <ErrorMsg className="profile-error">{t("profileFlyout.noUser")}</ErrorMsg>
            <Trans i18nKey={'profileFlyout.description'}>
              <Hyperlink href={Config.contextHelpUrls.rolesAndPermissions} target="_blank">{t('profileFlyout.learnMore')}</Hyperlink>
              about roles and permisions 
            </Trans>
          </div>
        }
        {
          user &&
          <div className="profile-container">
            <div className="profile-header">
              <h2>{user.email}</h2>
              <Grid className="profile-header-grid">
                <Row>
                  <Cell className="col-7">
                    <Trans i18nKey={'profileFlyout.description'}>
                      <Hyperlink href={Config.contextHelpUrls.rolesAndPermissions} target="_blank">{t('profileFlyout.learnMore')}</Hyperlink>
                      about roles and permisions
                    </Trans>
                  </Cell>
                  <Cell className="col-3"><Btn primary={true} onClick={logout}>{t('profileFlyout.logout')}</Btn></Cell>
                </Row>
              </Grid>
            </div>

            <Section.Container>
              <Section.Header>{t('profileFlyout.tenants.tenantHeader')}</Section.Header>
              <Section.Content>
                <div>
                  {currentTenant ? "Current: " + processTenantDisplayValue(currentTenant) : ""}
                </div>
                {
                  /* Create the list of available tenants if there are any */
                  (!tenants || tenants.length === 0)
                    ? t('profileFlyout.tenants.noTenant')
                    :
                    <Grid>
                      <Row>
                        <Cell>{t('profileFlyout.tenants.tenantNameColumn')}</Cell>
                        <Cell>{t('profileFlyout.tenants.tenantRoleColumn')}</Cell>
                        <Cell>{t('profileFlyout.tenants.tenantActionColumn')}</Cell>
                      </Row>
                      {
                        tenants.map((tenant, idx) =>
                          <Row key={tenant.id}>
                            {/* Make the tenant display value a button if it is not the active tenant, the button will switch the user to the selected tenant */}
                            <Cell>{(tenant.id === currentTenant) ? tenant.displayName : <a onClick={() => switchTenant(tenant.id)} href="#">{tenant.displayName}</a>}</Cell>
                            <Cell>{tenant.role}</Cell>
                            <Cell>
                              {(tenant.id == currentTenant && tenants.length > 1)
                              ?
                              <Protected permission={permissions.deleteTenant}>
                                <Btn className="delete-tenant-button" primary={true} onClick=
                                  {() => deleteTenantThenSwitch(idx != 0 ? tenants[idx - 1].id : tenants[idx + 1].id)}>
                                  {t('profileFlyout.tenants.deleteTenant')}
                                </Btn>
                              </Protected>
                              :
                              ""}
                            </Cell>
                          </Row>
                        )
                      }
                    </Grid>
                }
                <Grid>
                  <Cell id="create-tenant-cell">
                    <Btn className="create-tenant-button" primary={true} onClick=
                      {() =>
                        createTenant().subscribe(r => fetchTenants())
                      }>
                      {t('profileFlyout.tenants.createTenant')}
                    </Btn>
                  </Cell>
                </Grid>
              </Section.Content>
            </Section.Container>

            <Section.Container>
              <Section.Header>{t('profileFlyout.roles')}</Section.Header>
              <Section.Content>
                {
                  (roleArray.length === 0)
                    ? t('profileFlyout.noRoles')
                    :
                    <Grid>
                      {
                        roleArray.map((roleName, idx) =>
                          <Row key={idx}>
                            <Cell>{roleName}</Cell>
                          </Row>
                        )
                      }
                    </Grid>
                }
              </Section.Content>
            </Section.Container>

            <Section.Container>
              <Section.Header>{t('profileFlyout.permissions')}</Section.Header>
              <Section.Content>
                {
                  (permissionArray.length === 0)
                    ? t('profileFlyout.noPermissions')
                    :
                    <Grid>
                      {
                        permissionArray.map((permissionName, idx) =>
                          <Row key={idx}>
                            <Cell>{getEnumTranslation(t, 'permissions', permissionName)}</Cell>
                          </Row>
                        )
                      }
                    </Grid>
                }
              </Section.Content>
            </Section.Container>
            {
              (global.DeploymentConfig.developmentMode)
                ?
                <Section.Container>
                  <Section.Header>Development Variables</Section.Header>
                  <Section.Content>
                    <Grid>
                      id_token: <br/>{
                        user.token
                      }
                    </Grid>
                    <Grid>
                      payload: <br/>{
                        JSON.stringify(jwt_decode(user.token), null, 2)
                      }
                    </Grid>
                  </Section.Content>
                </Section.Container>
                : ''
            }
            <BtnToolbar>
              <Btn svg={svgs.cancelX} onClick={onClose}>{t('profileFlyout.close')}</Btn>
            </BtnToolbar>
          </div>
        }
      </div>
    </Flyout.Container>
  );
}
