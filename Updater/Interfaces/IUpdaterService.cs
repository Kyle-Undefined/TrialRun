using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Interfaces
{
    public interface IUpdaterService
    {
        Task InitializeAsync(string path);
    }
}