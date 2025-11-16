# Data Type Migration Guide

## Critical Changes Needed

The database uses:
- `users.id`: `uniqueidentifier` (GUID) - ✅ Fixed in models
- `Vehicles.id`: `int IDENTITY` - ✅ Fixed in models  
- `favorites.vehicleId`: `int` - ✅ Fixed in models
- `favorites.userId`: `uniqueidentifier` - ✅ Fixed in models

## Services That Need Updates

All services need to change:
- `string userId` → `Guid userId`
- `string vehicleId` → `int vehicleId` (for vehicles)
- `string vehicleId` → `int vehicleId` (for favorites)

## Files to Update

### Services
- AuthService.cs - UserId handling
- VehicleService.cs - VehicleId (int), UserId (Guid)
- SubscriptionService.cs - UserId (Guid)
- AdminService.cs - UserId (Guid), VehicleId (int)
- NotificationService.cs - UserId (Guid)
- UserService.cs - UserId (Guid)

### DTOs
- VehicleDTOs.cs - VehicleId should be int
- AuthDTOs.cs - UserId should be Guid (or keep string for API)

### Controllers
- All controllers need to parse Guid from string for UserId
- All controllers need to parse int from string for VehicleId

## Implementation Strategy

1. Keep DTOs using string for API compatibility
2. Convert string to Guid/int in controllers
3. Services use Guid/int internally
4. Convert back to string in DTOs for responses

