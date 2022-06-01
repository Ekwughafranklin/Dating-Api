using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext dataContext,IMapper mapper)
        {
            _dataContext = dataContext;
            _mapper = mapper;
        }
        public void AddMessage(Message message)
        {
            _dataContext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _dataContext.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
           return await _dataContext.Messages
                .Include(u=>u.Sender)
                .Include(u=>u.Recepient)
               .SingleOrDefaultAsync(x=>x.Id==id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _dataContext.Messages
                   .OrderByDescending(m => m.MessageSent)
                   .AsQueryable();

            query = messageParams.Container switch
            { 
                "Inbox" => query.Where(u => u.Recepient.UserName == messageParams.Username && u.RecepientDeleted==false),
                "Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username && u.SenderDeleted==false),
                _=> query.Where(u => u.Recepient.UserName == messageParams.Username && u.RecepientDeleted==false && u.DateRead==null)

            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(messages,messageParams.PageNumber,messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await _dataContext.Messages
                .Include(u=>u.Sender).ThenInclude(p=>p.Photos)
                .Include(u=>u.Recepient).ThenInclude(p=>p.Photos)
                .Where(m => m.Recepient.UserName == currentUsername && m.RecepientDeleted==false && 
                m.Sender.UserName==recipientUsername || m.Recepient.UserName==recipientUsername &&
                m.Sender.UserName==currentUsername && m.SenderDeleted==false)
                .OrderBy(m=>m.MessageSent)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.Recepient.UserName == currentUsername).ToList();

            if(unreadMessages.Any())
            {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.Now;

                }

                await _dataContext.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }
    }
}
