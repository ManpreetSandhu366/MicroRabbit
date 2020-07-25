using MicroRabbit.Banking.Application.Interfaces;
using MicroRabbit.Banking.Application.Models;
using MicroRabbit.Banking.Domain.Commands;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Banking.Domain.Models;
using MicroRabbit.Domain.Core.Bus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MicroRabbit.Banking.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IEventBus _bus;
        private readonly IAccountRepository _accountRepository;

        public AccountService(
            IEventBus bus,
            IAccountRepository accountRepository
            )
        {
            _bus = bus;
            _accountRepository = accountRepository;
        }

        public IEnumerable<Account> GetAccounts()
        {
            return _accountRepository.GetAccounts();
        }

        public void Transfer(AccountTransfer accountTransfer)
        {
            CreateTransferCommand createTransferCommand = new CreateTransferCommand(
                from: accountTransfer.FromAccount,
                to: accountTransfer.ToAccount,
                amount: accountTransfer.Amount
                );
            _bus.SendCommand(createTransferCommand);
        }
    }
}
