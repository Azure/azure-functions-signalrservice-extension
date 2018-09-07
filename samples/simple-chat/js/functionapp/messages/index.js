// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req) {
  const message = req.body;
  const recipient = req.query.recipient;
  const recipientUserIds = [];

  if (recipient) {
    recipientUserIds.push(recipient);
    message.text = "(private message) " + message.text;
  }
  context.bindings.signalRMessages = [{
    "userIds": recipientUserIds,
    "target": "newMessage",
    "arguments": [ message ]
  }];
  context.done();
};