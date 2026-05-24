using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
   public interface IUnitOfWork : IAsyncDisposable
    {
        ISeatRepository Seats { get; }
       

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
