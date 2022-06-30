using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;

        public MessageHub(IMapper mapper,IUnitOfWork unitOfWork, IHubContext<PresenceHub> presenceHub,
            PresenceTracker tracker)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _presenceHub = presenceHub;
            _tracker = tracker;
        }
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group= await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup",group);
            var messages = await _unitOfWork.messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);
            if (_unitOfWork.HasChanges()) await _unitOfWork.Complete();
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
           var group= await RemoveFromMesssageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDto.RecepientUsername.ToLower())
                throw new HubException("You cannot send messages to Yourself");

            var sender = await _unitOfWork.userRepository.GetUserByUserNameAsync(username);
            var recepient = await _unitOfWork.userRepository.GetUserByUserNameAsync(createMessageDto.RecepientUsername);

            if (recepient == null) throw new HubException("User Not Found");

            var message = new Message
            {
                Sender = sender,
                Recepient = recepient,
                SenderUsername = sender.UserName,
                RecepientUsername = recepient.UserName,
                Content = createMessageDto.Content
            };
            var groupName = GetGroupName(sender.UserName, recepient.UserName);
            var group = await _unitOfWork.messageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recepient.UserName))
            {

                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _tracker.GetConnectionsForUser(recepient.UserName);
                if (connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                        new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _unitOfWork.messageRepository.AddMessage(message);
            if (await _unitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }

        }

        private async Task<Group> AddToGroup( string groupName)
        {
            var group = await _unitOfWork.messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());
            if (group == null)
            {
                group = new Group(groupName);
                _unitOfWork.messageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);

            if(await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to join group");
        }
        private async Task<Group> RemoveFromMesssageGroup()
        {
            var group = await _unitOfWork.messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection=group.Connections.FirstOrDefault(x=>x.ConnectionId==Context.ConnectionId);
            _unitOfWork.messageRepository.RemoveConnection(connection);
            if(await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to remove from group");
        }
        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}
