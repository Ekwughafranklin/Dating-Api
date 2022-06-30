using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
           _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUsername();

            if (username == createMessageDto.RecepientUsername.ToLower())
                return BadRequest("You cannot send messages to Yourself");

            var sender = await _unitOfWork.userRepository.GetUserByUserNameAsync(username);
            var recepient = await _unitOfWork.userRepository.GetUserByUserNameAsync(createMessageDto.RecepientUsername);

            if (recepient == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recepient = recepient,
                SenderUsername = sender.UserName,
                RecepientUsername = recepient.UserName,
                Content = createMessageDto.Content
            };

            _unitOfWork.messageRepository.AddMessage(message);
            if (await _unitOfWork.Complete()) return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to save message");
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();
            var messages = await _unitOfWork.messageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();
            var message = await _unitOfWork.messageRepository.GetMessage(id);
            if (message.Sender.UserName != username && message.Recepient.UserName != username) return Unauthorized();
            if (message.Sender.UserName == username) message.SenderDeleted= true;
            if (message.Recepient.UserName == username) message.RecepientDeleted= true;

            if (message.SenderDeleted && message.RecepientDeleted) _unitOfWork.messageRepository.DeleteMessage(message);
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Problem deleting the message");
          
        }
    }
}
