#!/bin/bash
# ============================================================
# SkyBooker — Day 1 Setup Script (Bash — Linux/macOS)
# Usage: chmod +x setup-day1.sh && ./setup-day1.sh
# ============================================================

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}╔══════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║     SkyBooker — Day 1 Setup Script          ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════════╝${NC}"
echo ""

# Step 1 — .NET check
echo -e "${YELLOW}► Checking .NET SDK...${NC}"
dotnet --version || { echo -e "${RED}✗ .NET 8 SDK not found! Install from https://dotnet.microsoft.com${NC}"; exit 1; }
echo -e "${GREEN}  ✓ .NET SDK ready${NC}"

# Step 2 — Restore
echo ""
echo -e "${YELLOW}► Restoring NuGet packages...${NC}"
dotnet restore SkyBooker.sln || { echo -e "${RED}✗ Restore failed!${NC}"; exit 1; }
echo -e "${GREEN}  ✓ Packages restored${NC}"

# Step 3 — Build Auth API
echo ""
echo -e "${YELLOW}► Building SkyBooker.Auth.API...${NC}"
dotnet build SkyBooker.Auth.API/SkyBooker.Auth.API.csproj -c Debug --no-restore || {
    echo -e "${RED}✗ Build failed!${NC}"; exit 1;
}
echo -e "${GREEN}  ✓ Build succeeded${NC}"

# Step 4 — dotnet-ef
echo ""
echo -e "${YELLOW}► Checking dotnet-ef tool...${NC}"
if ! dotnet ef --version > /dev/null 2>&1; then
    echo "  Installing dotnet-ef..."
    dotnet tool install --global dotnet-ef
fi
echo -e "${GREEN}  ✓ dotnet-ef ready${NC}"

# Step 5 — Migration
echo ""
echo -e "${YELLOW}► Creating EF Core migration...${NC}"
cd SkyBooker.Auth.API
dotnet ef migrations add InitialCreate --output-dir Migrations || {
    echo -e "${RED}✗ Migration failed! Is SQL Server running?${NC}"; cd ..; exit 1;
}
echo -e "${GREEN}  ✓ Migration created${NC}"

# Step 6 — Apply
echo ""
echo -e "${YELLOW}► Applying database migration...${NC}"
dotnet ef database update || {
    echo -e "${RED}✗ DB update failed! Check connection string.${NC}"; cd ..; exit 1;
}
echo -e "${GREEN}  ✓ Database created${NC}"
cd ..

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║        ✅  Day 1 Setup Complete!             ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${CYAN}Next:${NC}"
echo "  dotnet run --project SkyBooker.Auth.API"
echo "  Open: http://localhost:5001  (Swagger UI)"
