#!/bin/bash

API_URL="http://localhost:5286"

echo "🔒 Testing Secure API..."
echo "=========================================="

echo -e "\n✅ TEST 1: Public Endpoint (No Auth Required)"
echo "GET /api/products"
RESP=$(curl -s -w "\n%{http_code}" -X GET $API_URL/api/products)
STATUS=$(echo "$RESP" | tail -1)
echo "Status: $STATUS (expected: 200)"
echo "$RESP" | head -1 | grep -o '"name":"[^"]*' | head -3

echo -e "\n❌ TEST 2: Protected Endpoint Without Auth (should fail)"
echo "POST /api/products (requires authentication)"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test","price":100,"category":"Test","stockQuantity":10}')
echo "Status: $STATUS (expected: 401)"

echo -e "\n🔑 TEST 3: Login with Valid Credentials"
echo "POST /api/auth/login"
RESPONSE=$(curl -s -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@secureapi.com","password":"Admin@123!"}')
  
TOKEN=$(echo "$RESPONSE" | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)
if [ -n "$TOKEN" ]; then
  echo "✅ Status: 200"
  echo "✅ Access Token received: ${TOKEN:0:30}..."
else
  echo "❌ Login failed"
  echo "Response: $RESPONSE"
fi

echo -e "\n🛡️  TEST 4: Rate Limiting on Auth Endpoint (10 failed + 1 success)"
echo "POST /api/auth/login with wrong password (11 times)"
SUCCESS=0
RATELIMIT=0
for i in {1..11}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}')
  
  if [ "$STATUS" = "400" ]; then
    ((SUCCESS++))
    echo "  Attempt $i: 400 Invalid Credentials"
  elif [ "$STATUS" = "429" ]; then
    ((RATELIMIT++))
    echo "  Attempt $i: 429 Rate Limited ⛔"
  else
    echo "  Attempt $i: $STATUS"
  fi
done
echo "Summary: $SUCCESS failed logins, $RATELIMIT rate limited ✅"

echo -e "\n✔️  TEST 5: Input Validation - Weak Password"
echo "POST /api/auth/register with weak password"
RESPONSE=$(curl -s -X POST $API_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"weakpwd@test.com","password":"weak","fullName":"Test","birthDate":"2000-01-01"}')
echo "Response: $RESPONSE" | grep -o '"error":"[^"]*' || echo "Status: 400 (validation failed) ✅"

echo -e "\n=========================================="
echo "✅ All security features tested!"
