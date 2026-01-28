using Microsoft.EntityFrameworkCore;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.Entites;
using PaymentGateway.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Services
{
    public class WalletService:IWalletService
    {
        private readonly PaymentGatewayDbCOntext _context;

        public WalletService(PaymentGatewayDbCOntext context)
        {
            _context = context;
        }

        public async Task<Wallet> CreateWalletAsync(Guid userid, string currency)
        {
            var walletExist = await _context.Wallets.AnyAsync(x => x.UserId == userid && x.Currency == currency);

            if (walletExist)
                throw new Exception("Wallet already exist");

            var newWallet = Wallet.Create(userid, currency);
            _context.Wallets.Add(newWallet);
            await _context.SaveChangesAsync();

            return newWallet;
        }
    }
}
