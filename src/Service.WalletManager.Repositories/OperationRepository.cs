﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Service.WalletManager.Domain.Models;
using Service.WalletManager.Domain.Repositories;
using Service.WalletManager.Repositories.DbContexts;
using Service.WalletManager.Repositories.Entities;

namespace Service.WalletManager.Repositories
{
    public class OperationRepository : IOperationRepository
    {
        private readonly DbContextOptionsBuilder<WalletManagerContext> _dbContextOptionsBuilder;

        public OperationRepository(DbContextOptionsBuilder<WalletManagerContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task SetAsync(CreateOperation operation)
        {
            using (var context = new WalletManagerContext(_dbContextOptionsBuilder.Options))
            {
                var entity = MapToOperationEntity(operation);
                context.Operations.Add(entity);

                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Operation>> GetAllForBlockchainAsync(string blockchainId, int skip, int take)
        {
            using (var context = new WalletManagerContext(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x =>
                        x.BlockchianId == blockchainId )
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        public async Task<IEnumerable<Operation>> GetAsync(DepositWalletKey key, int skip, int take)
        {
            using (var context = new WalletManagerContext(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x => 
                                                x.BlockchainAssetId == key.BlockchainAssetId &&
                                                x.BlockchianId == key.BlockchainId &&
                                                x.WalletAddress == key.WalletAddress.ToLower(CultureInfo.InvariantCulture))
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        public async Task<IEnumerable<Operation>> GetAsync(string blockchainId, string walletAddress, int skip, int take)
        {
            using (var context = new WalletManagerContext(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x =>
                        x.BlockchianId == blockchainId &&
                        x.WalletAddress == walletAddress.ToLower(CultureInfo.InvariantCulture))
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        private static OperationEntity MapToOperationEntity(CreateOperation operation)
        {
            return new OperationEntity()
            {
                BlockchainAssetId = operation.Key.BlockchainAssetId,
                BlockchianId = operation.Key.BlockchainId,
                BalanceChange = operation.BalanceChange.ToString(),
                BlockNumber = operation.Block,
                WalletAddress = operation.Key.WalletAddress.ToLower(CultureInfo.InvariantCulture),
                OriginalWalletAddress = operation.Key.WalletAddress
            };
        }

        private static Operation MapFromOperationEntity(OperationEntity operation)
        {
            BigInteger.TryParse(operation.BalanceChange, out var balanceChange);

            return Operation.Create(
                new DepositWalletKey(operation.BlockchainAssetId, operation.BlockchianId, 
                operation.OriginalWalletAddress),
                balanceChange,
                operation.BlockNumber,
                operation.OperationId);
        }
    }
}
