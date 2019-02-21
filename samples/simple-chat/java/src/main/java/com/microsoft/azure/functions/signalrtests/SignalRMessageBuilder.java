package com.microsoft.azure.functions.signalrtests;

import com.microsoft.azure.functions.signalr.*;

public class SignalRMessageBuilder {
    private SignalRMessage message = new SignalRMessage();
    public SignalRMessageBuilder(String target) {
        super();
        message.target = target;
    }

    public SignalRMessageBuilder addArgument(Object arg) {
        message.arguments.add(arg);
        return this;
    }

    public SignalRMessage build() {
        return message;
    }
}