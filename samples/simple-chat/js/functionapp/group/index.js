// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req) {
  context.bindings.signalRMessages = [{
    "userId": req.query.recipient,
    "groupName": req.query.groupname,
    "action": req.query.action
  }];
  context.done();
};