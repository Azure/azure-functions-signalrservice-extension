package com.microsoft.azure.functions.signalrtests;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;
import com.microsoft.azure.functions.signalr.annotation.*;
import com.microsoft.azure.functions.signalr.*;

public class Functions {
    @FunctionName("negotiate")
    public SignalRConnectionInfo negotiate(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST, HttpMethod.GET },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Optional<String>> req,
            @SignalRConnectionInfoInput(name = "connectionInfo", hubName = "simplechat") SignalRConnectionInfo connectionInfo) {
                
        return connectionInfo;
    }

    @FunctionName("messages")
    @SignalROutput(name = "", hubName = "simplechat")
    public SignalRMessage sendMessage(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Object> req) {

        return new SignalRMessageBuilder("newMessage")
            .addArgument(req.getBody())
            .build();
    }
}
