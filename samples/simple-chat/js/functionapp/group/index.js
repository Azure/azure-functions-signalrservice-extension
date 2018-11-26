// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req) {
  context.bindings.signalRGroupActions = [{
    "userId": req.body.recipient,
    "groupName": req.body.groupname,
    "action": req.body.action
  }];
  context.done();
};