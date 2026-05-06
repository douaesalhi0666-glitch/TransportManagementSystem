using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class FragmentService
    {
        private readonly ApplicationDbContext _context;

        public FragmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FragmentResult>> GenerateFragments(int trajectoryId, int busCapacity = 20)
        {
            var fragments = new List<FragmentResult>();

            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == trajectoryId)
                .OrderBy(s => s.TS_OrderIndex)
                .Select(s => new StopWorkers
                {
                    StopId = s.TS_Id,
                    StopName = s.TS_Name,
                    OrderIndex = s.TS_OrderIndex,
                    Latitude = s.TS_Latitude,
                    Longitude = s.TS_Longitude,
                    WorkerCount = _context.Personnel
                        .Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                })
                .ToListAsync();

            if (!stops.Any())
                return fragments;

            int fragmentCounter = 1;
            int currentTotal = 0;
            var currentStops = new List<StopWorkers>();

            foreach (var stop in stops)
            {
                if (currentTotal + stop.WorkerCount <= busCapacity)
                {
                    currentStops.Add(stop);
                    currentTotal += stop.WorkerCount;
                }
                else
                {
                    if (currentStops.Any())
                    {
                        fragments.Add(new FragmentResult
                        {
                            FragmentId = fragmentCounter,
                            FragmentCode = $"FRG-{trajectoryId}-{fragmentCounter}",
                            FragmentName = $"Fragment {fragmentCounter} - {currentStops.First().StopName} to {currentStops.Last().StopName}",
                            TotalWorkers = currentTotal,
                            Stops = new List<StopWorkers>(currentStops)
                        });
                        fragmentCounter++;
                    }

                    currentStops = new List<StopWorkers> { stop };
                    currentTotal = stop.WorkerCount;
                }
            }

            if (currentStops.Any())
            {
                fragments.Add(new FragmentResult
                {
                    FragmentId = fragmentCounter,
                    FragmentCode = $"FRG-{trajectoryId}-{fragmentCounter}",
                    FragmentName = $"Fragment {fragmentCounter} - {currentStops.First().StopName} to {currentStops.Last().StopName}",
                    TotalWorkers = currentTotal,
                    Stops = new List<StopWorkers>(currentStops)
                });
            }

            return fragments;
        }

        public async Task<List<TrajectoryFragment>> SaveFragments(int trajectoryId, List<FragmentResult> fragments)
        {
            var savedFragments = new List<TrajectoryFragment>();

            foreach (var fragment in fragments)
            {
                var dbFragment = new TrajectoryFragment
                {
                    Trajectory_Id = trajectoryId,
                    Fragment_Code = fragment.FragmentCode,
                    Fragment_Name = fragment.FragmentName,
                    Total_Workers = fragment.TotalWorkers,
                    Status = "Active",
                    Created_At = DateTime.Now
                };
                _context.TrajectoryFragments.Add(dbFragment);
                await _context.SaveChangesAsync();

                int stopOrder = 1;
                foreach (var stop in fragment.Stops)
                {
                    var fragmentStop = new FragmentStop
                    {
                        Fragment_Id = dbFragment.Fragment_Id,
                        TS_Id = stop.StopId,
                        Stop_Order = stopOrder++,
                        Workers_At_Stop = stop.WorkerCount
                    };
                    _context.FragmentStops.Add(fragmentStop);
                }

                await _context.SaveChangesAsync();

                savedFragments.Add(dbFragment);
            }

            return savedFragments;
        }

        public async Task<bool> AssignBusToFragment(long busId, int fragmentId, DateTime startTime)
        {
            var bus = await _context.Buses.FindAsync(busId);
            var fragment = await _context.TrajectoryFragments.FindAsync(fragmentId);

            if (bus == null || fragment == null)
                return false;

            var assignment = new BusFragmentAssignment
            {
                Bus_Id = busId,
                Fragment_Id = fragmentId,
                Start_DateTime = startTime,
                Status = "Active"
            };
            _context.BusFragmentAssignments.Add(assignment);

            bus.Bus_Status = "On Route";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignDriverToFragment(long driverId, int fragmentId, DateTime startTime)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            var fragment = await _context.TrajectoryFragments.FindAsync(fragmentId);

            if (driver == null || fragment == null)
                return false;

            var assignment = new DriverFragmentAssignment
            {
                Driver_Id = driverId,
                Fragment_Id = fragmentId,
                Start_DateTime = startTime,
                Status = "Active"
            };
            _context.DriverFragmentAssignments.Add(assignment);

            driver.Driver_Status = "On Route";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object?> GetFragmentMapData(int fragmentId)
        {
            var fragment = await _context.TrajectoryFragments
                .FirstOrDefaultAsync(f => f.Fragment_Id == fragmentId);

            if (fragment == null) return null;

            var stops = await _context.FragmentStops
                .Where(fs => fs.Fragment_Id == fragmentId)
                .OrderBy(fs => fs.Stop_Order)
                .Join(_context.TrajectoryStops,
                    fs => fs.TS_Id,
                    ts => ts.TS_Id,
                    (fs, ts) => new
                    {
                        ts.TS_Id,
                        ts.TS_Name,
                        ts.TS_Latitude,
                        ts.TS_Longitude,
                        fs.Stop_Order,
                        fs.Workers_At_Stop
                    })
                .ToListAsync();

            var busAssignment = await _context.BusFragmentAssignments
                .Include(bf => bf.Bus)
                .FirstOrDefaultAsync(bf => bf.Fragment_Id == fragmentId && bf.Status == "Active");

            return new
            {
                fragment.Fragment_Id,
                fragment.Fragment_Code,
                fragment.Fragment_Name,
                fragment.Total_Workers,
                Stops = stops,
                AssignedBus = busAssignment?.Bus,
                BusStatus = busAssignment?.Status
            };
        }
    }

    // Helper classes
    public class StopWorkers
    {
        public int StopId { get; set; }
        public string StopName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int WorkerCount { get; set; }
    }

    public class FragmentResult
    {
        public int FragmentId { get; set; }
        public string FragmentCode { get; set; } = string.Empty;
        public string FragmentName { get; set; } = string.Empty;
        public int TotalWorkers { get; set; }
        public List<StopWorkers> Stops { get; set; } = new List<StopWorkers>();
    }
}