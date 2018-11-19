using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Taxi.Entities;
using Taxi.Helpers;
using Taxi.Models;
using Taxi.Models.Trips;

namespace Taxi.Services
{
    public interface ITripsRepository
    {
        List<Guid> GetCustomerIdsForDriverTrips(Guid driverId);

        Task<List<TripHistoryRouteNode>> GetTripRouteNodes(Guid tripId);

        bool RemoveTrip(Guid customerId);
        
        Trip GetTrip(Guid customerId, bool includeRoutes = false);

        PagedList<TripDto> GetNearTrips(double lon, double lat, PaginationParameters paginationParameters);

        Trip GetTripByDriver(Guid driverId, bool includeRoutes  = false);
        
        Task<bool> AddTripHistory(TripHistory tripHistory);

        Task<TripHistory> GetTripHistory(Guid id);

        PagedList<TripHistory> GetTripHistoriesForCustomer(Guid CustomerId, TripHistoryResourceParameters resourceParameters);

        PagedList<TripHistory> GetTripHistoriesForDriver(Guid DriverId, TripHistoryResourceParameters resourceParameters);
        Task<bool> UpdateTrip(Trip trip, PlaceDto from = null, PlaceDto to = null);
        Task<bool> AddNode(TripRouteNode node);
        void InsertTrip(Trip tripEntity, double lat1, double lon1, double lat2, double lon2);
        bool AddRefundRequest(RefundRequest refundRequest);
        bool AddContract(Contract contract);

        Contract GetContract(ulong id);
    }
}
   