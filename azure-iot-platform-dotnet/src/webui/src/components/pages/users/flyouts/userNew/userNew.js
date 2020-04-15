// Copyright (c) Microsoft. All rights reserved.

import React from 'react';
import update from 'immutability-helper';

import { IdentityGatewayService } from 'services';
import {
  permissions,
  toDiagnosticsModel
} from 'services/models';
import {
  copyToClipboard,
  int,
  LinkedComponent,
  svgs,
  Validator
} from 'utilities';
import {
  AjaxError,
  Btn,
  BtnToolbar,
  Flyout,
  FormControl,
  FormGroup,
  FormLabel,
  FormSection,
  Indicator,
  Protected,
  Radio,
  SectionDesc,
  SectionHeader,
  SummaryBody,
  SummaryCount,
  SummarySection,
  Svg
} from 'components/shared';

import './userNew.scss';
import Config from 'app.config';
import {Policies} from 'utilities'

const isEmailRegex = /^(?:[a-z0-9A-Z!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9A-Z!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])$/;
const emailAddress = x => x.match(isEmailRegex);
const stringToInt = x => x === '' || x === '-' ? x : int(x);

const userOptions = {
  labelName: 'users.flyouts.new.user.label',
  user: {
    labelName: 'users.flyouts.new.user.user',
    value: false
  },
  edgeUser: {
    labelName: 'users.flyouts.new.user.edgeUser',
    value: true
  }
};

const userRolesOptions = Policies.map(p => {
  return {
    "label": p.DisplayName,
    "value": p.Role
  }
});


export class UserNew extends LinkedComponent {
  constructor(props) {
    super(props);

    this.state = {
      isPending: false,
      error: undefined,
      successCount: 0,
      changesApplied: false,
      formData: {
        email: "",
        role: ""
      },
      provisionedUser: {}
    };

    // Linked components
    this.formDataLink = this.linkTo('formData');

    this.emailLink = this.formDataLink.forkTo('email')
      .check(Validator.notEmpty, () => this.props.t('users.flyouts.new.validation.required'))
      .check(emailAddress, () => this.props.t('users.flyouts.new.validation.invalid'))


    this.roleLink = this.formDataLink.forkTo('role')
    .map(({ value }) => value)
    .check(Validator.notEmpty, () => this.props.t('users.flyouts.new.validation.required'))
  }

  componentWillUnmount() {
    if (this.provisionSubscription) this.provisionSubscription.unsubscribe();
  }

  shouldComponentUpdate(nextProps, nextState) {
    const { formData } = nextState;
    // For setting rules. Like disable if x is true...

    // Update normally
    return true;
  }

  formIsValid() {
    return [
      this.emailLink,
      this.roleLink
    ].every(link => !link.error);
  }

  formControlChange = () => {
    if (this.state.changesApplied) {
      this.setState({
        successCount: 0,
        changesApplied: false,
        provisionedUser: {}
      });
    }
  }
  roleChange = (selectedObject) => {
    console.log(this.state.formData)
  }

  onFlyoutClose = (eventName) => {
    this.props.logEvent(toDiagnosticsModel(eventName, this.state.formData));
    this.props.onClose();
  }

  invite = (event) => {
    event.preventDefault();
    const { formData } = this.state;

    if (this.formIsValid()) {
      this.setState({ isPending: true, error: null });

      IdentityGatewayService.invite(this.state.formData.email, this.state.formData.role)
      .subscribe(
        function(user){
          this.setState({ successCount: this.state.successCount + 1 })
          this.props.insertUsers(user);
        }.bind(this),
        error => this.setState({ error, isPending: false, changesApplied: true }), // On Error
        () => this.setState({ isPending: false, changesApplied: true, confirmStatus: false }) // On Completed
      );

      this.props.logEvent(toDiagnosticsModel('Users_InviteClick', formData));

    }
  }

  getSummaryMessage() {
    const { t } = this.props;
    const { isPending, changesApplied } = this.state;

    if (isPending) {
      return t('users.flyouts.new.pending');
    } else if (changesApplied) {
      return t('users.flyouts.new.user.applySuccess');
    } else {
      return t('users.flyouts.new.affected');
    }
  }

  render() {
    const {
      t
    } = this.props;
    const {
      formData,
      provisionedUser,
      isPending,
      error,
      successCount,
      changesApplied
    } = this.state;

    const completedSuccessfully = changesApplied && !error;
    const summaryMessage = this.getSummaryMessage();
    console.log(permissions.inviteUsers)
    return (
      <Flyout header={t('users.flyouts.new.title')} t={t} onClose={() => this.onFlyoutClose('Users_TopXCloseClick')}>
        <Protected permission={permissions.inviteUsers}>
          <form className="users-new-container" onSubmit={this.invite}>
            <div className="users-new-content">
              <FormGroup>
                <FormLabel>{t(userOptions.labelName)}</FormLabel>
                <FormControl link={this.emailLink} type="text" onChange={this.formControlChange} />
              </FormGroup>

              <FormGroup>
                    <FormLabel>{t('users.flyouts.new.roles.label')}</FormLabel>
                    <FormControl
                      name="roleSelect"
                      link={this.roleLink}
                      ariaLabel={t('users.flyouts.new.roles.label')}
                      type="select"
                      options={userRolesOptions}
                      placeholder={t('users.flyouts.new.roles.hint')}
                      onChange={this.roleChange} />
              </FormGroup>
            </div>

            {error && <AjaxError className="users-new-error" t={t} error={error} />}
            {
              !changesApplied &&
              <BtnToolbar>
                <Btn primary={true} disabled={isPending || !this.formIsValid()} type="submit">{t('users.flyouts.new.user.apply')}</Btn>
                <Btn svg={svgs.cancelX} onClick={() => this.onFlyoutClose('Users_CancelClick')}>{t('users.flyouts.new.cancel')}</Btn>
              </BtnToolbar>
            }
            {
              !!changesApplied &&
              <>

              <SummarySection>
                <SectionHeader>{t('users.flyouts.new.summaryHeader')}</SectionHeader>
                <SummaryBody>
                  <SectionDesc>{summaryMessage}</SectionDesc>
                  {this.state.isPending && <Indicator />}
                  {completedSuccessfully && <Svg className="summary-icon" path={svgs.apply} />}
                </SummaryBody>
              </SummarySection>
                <BtnToolbar>
                  <Btn svg={svgs.cancelX} onClick={() => this.onFlyoutClose('Users_CloseClick')}>{t('users.flyouts.new.close')}</Btn>
                </BtnToolbar>
              </>
            }
          </form>
        </Protected>
      </Flyout>
    );
  }
}
