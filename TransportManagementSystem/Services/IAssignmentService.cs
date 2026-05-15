using System.Threading.Tasks;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public interface IAssignmentService
    {
        Task<bool> AutoAssignNonMotorizedPersonnel(Personnel personnel);
    }
}