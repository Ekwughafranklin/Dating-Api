using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController :ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly DataContext _datacontext;

        public UsersController(ILogger<UsersController> logger,DataContext datacontext )
        {
            _logger = logger;
            _datacontext=datacontext;
        }


        [HttpGet]
        public async Task <ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
        var users=await _datacontext.Users.ToListAsync();
            return users;
        }
        [HttpGet("{id}")]
         public ActionResult<AppUser> GetUser(int id)
        {
        return _datacontext.Users.Find(id);

        }

       

    }
}