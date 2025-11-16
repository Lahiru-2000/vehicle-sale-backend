# Testing Guide for .NET Backend

## Prerequisites

1. **.NET SDK** (8.0 or later) installed
2. **SQL Server** running with `vehicle_hub` database
3. **Database connection** configured in `appsettings.json`

## Step 1: Verify Database Connection

Check your `appsettings.json` has the correct connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MSI\\SQLEXPRESS;Database=vehicle_hub;User Id=root;Password=root;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

## Step 2: Build the Project

```bash
cd VehiclePricePrediction.API
dotnet restore
dotnet build
```

If build succeeds, you're ready to run!

## Step 3: Run the Backend

```bash
dotnet run
```

The API should start on:
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `http://localhost:5000/swagger` (in development)

## Step 4: Test Endpoints

### Option A: Using Swagger UI (Easiest)

1. Open `http://localhost:5000/swagger` in your browser
2. You'll see all available endpoints
3. Click "Try it out" on any endpoint
4. Fill in parameters and click "Execute"

### Option B: Using Postman/Thunder Client

### Option C: Using curl (Command Line)

## Test Scenarios

### 1. Test Authentication

#### Register a New User
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "Test123!",
    "phone": "1234567890"
  }'
```

**Expected Response:**
```json
{
  "message": "User registered successfully",
  "user": {
    "id": "...",
    "email": "test@example.com",
    "name": "Test User",
    "role": "user"
  },
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

**Save the token** for authenticated requests!

#### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

#### Get Current User (Me)
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 2. Test Vehicles

#### Get All Vehicles (Public)
```bash
curl -X GET "http://localhost:5000/api/vehicles?status=approved&page=1&limit=10"
```

#### Create a Vehicle (Requires Auth)
```bash
curl -X POST http://localhost:5000/api/vehicles \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Toyota Camry 2020",
    "brand": "Toyota",
    "model": "Camry",
    "year": 2020,
    "price": 2500000,
    "type": "car",
    "fuelType": "petrol",
    "transmission": "automatic",
    "condition": "USED",
    "mileage": 50000,
    "description": "Well maintained car",
    "images": ["image1.jpg"],
    "contactInfo": {
      "phone": "1234567890",
      "email": "seller@example.com"
    }
  }'
```

#### Get Vehicle by ID
```bash
curl -X GET http://localhost:5000/api/vehicles/1
```

### 3. Test Favorites

#### Get User's Favorites
```bash
curl -X GET http://localhost:5000/api/favorites \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

#### Add to Favorites
```bash
curl -X POST http://localhost:5000/api/favorites \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "vehicleId": "1"
  }'
```

#### Check if Favorited
```bash
curl -X GET "http://localhost:5000/api/favorites/check?vehicleId=1" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

#### Remove from Favorites
```bash
curl -X DELETE "http://localhost:5000/api/favorites?vehicleId=1" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 4. Test Subscriptions

#### Get Subscription Plans
```bash
curl -X GET http://localhost:5000/api/subscriptions/plans
```

#### Get User Subscription
```bash
curl -X GET http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

#### Create Subscription
```bash
curl -X POST http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "planType": "basic",
    "paymentMethod": "card",
    "transactionId": "txn_123"
  }'
```

### 5. Test Admin Endpoints

#### Admin Login
```bash
curl -X POST http://localhost:5000/api/auth/admin-login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123!"
  }'
```

**Note:** You need an admin user first. Create one in the database or use existing admin credentials.

#### Get All Users (Admin)
```bash
curl -X GET http://localhost:5000/api/admin/users \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

#### Get Admin Stats
```bash
curl -X GET http://localhost:5000/api/admin/stats \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

#### Get Analytics
```bash
curl -X GET http://localhost:5000/api/admin/analytics \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

#### Approve Vehicle
```bash
curl -X POST http://localhost:5000/api/admin/vehicles/1/approve \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

#### Toggle User Block
```bash
curl -X POST http://localhost:5000/api/admin/users/USER_ID/toggle-block \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

### 6. Test Stats

#### Get Active Users Count
```bash
curl -X GET http://localhost:5000/api/stats/users
```

## Common Issues & Solutions

### Issue 1: CORS Error
**Solution:** Already fixed! CORS is configured correctly. If you still see errors, make sure:
- Frontend is calling `http://localhost:5000` (not https)
- Backend is running on port 5000

### Issue 2: Database Connection Error
**Solution:** 
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Check database `vehicle_hub` exists

### Issue 3: 401 Unauthorized
**Solution:**
- Make sure you're including the `Authorization: Bearer TOKEN` header
- Token might be expired (tokens last 7 days by default)
- Login again to get a new token

### Issue 4: 500 Internal Server Error
**Solution:**
- Check the console output for detailed error messages
- Verify database tables exist
- Check if all required fields are provided

### Issue 5: Guid/Int Conversion Errors
**Solution:** Already fixed! All services now properly convert between:
- API strings ↔ Guid (for UserId)
- API strings ↔ int (for VehicleId)

## Testing with Frontend

### Step 1: Update Frontend Environment

Create/update `.env.local` in your Next.js project:

```env
NEXT_PUBLIC_USE_DOTNET_BACKEND=true
NEXT_PUBLIC_API_URL=http://localhost:5000
```

### Step 2: Start Frontend

```bash
npm run dev
```

### Step 3: Test in Browser

1. Open `http://localhost:3000`
2. Try registering a new user
3. Try logging in
4. Browse vehicles
5. Add to favorites
6. Test admin features (if you have admin account)

## Expected Behavior

✅ **Working:**
- User registration and login
- Vehicle CRUD operations
- Favorites (add/remove/check)
- Subscriptions (plans, create, cancel)
- Admin basic operations (users list, approve/reject vehicles)
- Stats and Analytics

⚠️ **Not Yet Implemented:**
- Admin subscription plans management (CRUD)
- Admin permissions management
- Settings management
- Image upload
- Some advanced admin features

## Next Steps After Testing

1. **If everything works:** Continue implementing remaining features
2. **If errors found:** Report them and we'll fix
3. **If performance issues:** We can optimize queries

## Quick Test Checklist

- [ ] Backend starts without errors
- [ ] Swagger UI loads at `/swagger`
- [ ] Can register a new user
- [ ] Can login and get token
- [ ] Can get current user (me)
- [ ] Can get vehicles list
- [ ] Can create a vehicle (with auth)
- [ ] Can get vehicle by ID
- [ ] Can add to favorites
- [ ] Can get favorites list
- [ ] Can get subscription plans
- [ ] Can get admin stats (with admin token)
- [ ] Can get analytics (with admin token)
- [ ] Can approve vehicle (with admin token)

## Need Help?

If you encounter any errors:
1. Check the console output
2. Check Swagger UI for endpoint details
3. Verify your database connection
4. Make sure you're using the correct token for authenticated endpoints

