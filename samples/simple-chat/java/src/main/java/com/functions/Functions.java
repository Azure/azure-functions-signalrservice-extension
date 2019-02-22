package com.functions;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;
import com.microsoft.azure.functions.signalr.annotation.*;
import com.microsoft.azure.functions.signalr.*;
import com.google.gson.*;

public class Functions {
    @FunctionName("negotiate")
    public SignalRConnectionInfo negotiate(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST, HttpMethod.GET },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Optional<String>> req,
            @SignalRConnectionInfoInput(name = "connectionInfo", hubName = "simplechat", userId = "{headers.x-ms-signalr-userid}") SignalRConnectionInfo connectionInfo) {
                
        return connectionInfo;
    }

    @FunctionName("messages")
    public void sendMessage(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Object> req,
            @SignalROutput(name = "sendMessages", hubName = "simplechat") OutputBinding<SignalRMessage> signalRMessage) {
        
        String body = req.getBody().toString();
        Gson g = new Gson();
        ChatMessage chatMessage = g.fromJson(body, ChatMessage.class);

        SignalRMessage message = new SignalRMessage();
        message.target = "newMessage";
        message.groupName = chatMessage.groupname;
        message.userId = chatMessage.recipient;
        message.arguments.add(req.getBody());
        signalRMessage.setValue(message);
    }

    @FunctionName("addToGroup")
    public void addToGroup(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Object> req,
            @SignalROutput(name = "addToGroup", hubName = "simplechat") OutputBinding<SignalRGroupAction> signalRGroupAction) {
        
        String body = req.getBody().toString();
        Gson g = new Gson();
        ChatMessage chatMessage = g.fromJson(body, ChatMessage.class);

        SignalRGroupAction groupAction = new SignalRGroupAction();
        groupAction.groupName = chatMessage.groupname;
        groupAction.userId = chatMessage.recipient;
        groupAction.action = "add";
        signalRGroupAction.setValue(groupAction);
    }

    @FunctionName("removeFromGroup")
    public void removeFromGroup(
            @HttpTrigger(
                name = "req", 
                methods = { HttpMethod.POST },
                authLevel = AuthorizationLevel.ANONYMOUS) 
                HttpRequestMessage<Object> req,
            @SignalROutput(name = "removeFromGroup", hubName = "simplechat") OutputBinding<SignalRGroupAction> signalRGroupAction) {
        
        String body = req.getBody().toString();
        Gson g = new Gson();
        ChatMessage chatMessage = g.fromJson(body, ChatMessage.class);

        SignalRGroupAction groupAction = new SignalRGroupAction();
        groupAction.groupName = chatMessage.groupname;
        groupAction.userId = chatMessage.recipient;
        groupAction.action = "remove";
        signalRGroupAction.setValue(groupAction);
    }

    public static class ChatMessage
    {
        public String sender;
        public String text;
        public String groupname;
        public String recipient;
        public Boolean isPrivate;
    }
}

