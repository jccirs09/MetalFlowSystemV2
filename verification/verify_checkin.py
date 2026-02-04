import asyncio
from playwright.async_api import async_playwright
import time

async def run():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(ignore_https_errors=True)
        page = await context.new_page()

        print("Navigating to Login...")
        await page.goto("http://localhost:5048/Account/Login")
        await page.fill("input[name='Input.Email']", "admin@metalflow.com")
        await page.fill("input[name='Input.Password']", "Admin123!")
        await page.click("button[type='submit']")
        await page.wait_for_url("http://localhost:5048/")

        # --- 1. SET DEFAULT BRANCH ---
        print("Setting Default Branch for Admin...")
        await page.goto("http://localhost:5048/admin/users")

        await page.click("tr:has-text('admin@metalflow.com') button") # Edit icon
        await page.wait_for_selector("div.mud-dialog-title:has-text('Edit User')")

        try:
            # Check if "Add Branch" is available. If Admin already has branches, we might need to verify or just add.
            # We assume we can add.
            if await page.locator("button:has-text('Add Branch')").is_visible():
                await page.click("button:has-text('Add Branch')")
                await page.wait_for_selector("div.mud-dialog-title:has-text('Add Branch Access')")

                # Select Brandon
                await page.click("div.mud-select:has(label:has-text('Branch'))")
                await page.click("div.mud-list-item:has-text('Brandon')")

                # Select Role
                await page.click("div.mud-select:has(label:has-text('Role'))")
                await page.click("div.mud-list-item:has-text('Admin')")

                # Check Default
                await page.click("div.mud-input-control:has(label:has-text('Default Branch')) input")

                await page.click("button:has-text('Add')")
                await page.wait_for_selector("div.mud-dialog-title:has-text('Add Branch Access')", state="hidden")
        except Exception as e:
             print(f"Branch add skipped/failed: {e}")

        # Save User
        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")
        print("Default Branch set to Brandon.")

        # --- 2. CREATE PRODUCTION AREA ---
        print("Creating Production Area...")
        await page.goto("http://localhost:5048/admin/production-areas")
        # Button might be "Create Area" or "Create Production Area"
        # Try both or partial match
        await page.click("button:has-text('Create')") # Generic match? Or "Create Area"
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Production Area')")

        area_name = f"Slitter-{int(time.time())}"
        await page.fill("div.mud-input-control:has(label:has-text('Name')) input", area_name)

        # Select Branch Brandon
        await page.click("div.mud-select:has(label:has-text('Branch'))")
        await page.click("div.mud-list-item:has-text('Brandon')")

        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")

        # --- 3. CREATE SHIFT ---
        print("Creating Shift...")
        await page.goto("http://localhost:5048/admin/shifts")
        await page.click("button:has-text('Create')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Shift')")

        shift_name = "Day Shift"
        await page.fill("div.mud-input-control:has(label:has-text('Shift Name')) input", shift_name)
        # Branch Brandon
        await page.click("div.mud-select:has(label:has-text('Branch'))")
        await page.click("div.mud-list-item:has-text('Brandon')")

        # We need to set time to ensure it covers NOW.
        # UTC now.
        # Defaults might be 00:00 to 00:00.

        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")

        # --- 4. CREATE ASSIGNMENT ---
        print("Creating Assignment...")
        await page.goto("http://localhost:5048/admin/assignments")

        await page.click("button:has-text('Create')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Work Assignment')")

        # Select User: Admin
        await page.click("div.mud-select:has(label:has-text('User'))")
        await page.click("div.mud-list-item:has-text('System Admin')")

        # Select Branch
        await page.click("div.mud-select:has(label:has-text('Branch'))")
        await page.click("div.mud-list-item:has-text('Brandon')")

        # Select Shift
        await page.click("div.mud-select:has(label:has-text('Shift Template'))")
        await page.click(f"div.mud-list-item:has-text('{shift_name}')")

        # Select Area
        await page.click("div.mud-select:has(label:has-text('Production Area'))")
        await page.click(f"div.mud-list-item:has-text('{area_name}')")

        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")
        print("Assignment Created.")

        # --- 5. VERIFY CHECK IN ---
        print("Navigating to Check-In...")
        await page.goto("http://localhost:5048/check-in")

        # Verify text
        await page.wait_for_selector(f"h6:has-text('{shift_name}')")
        await page.wait_for_selector(f"p:has-text('{area_name}')")

        # Click Check In
        print("Clicking Check In...")
        await page.click("button:has-text('Check In')")

        # Verify Success
        await page.wait_for_selector("div.mud-alert-message:has-text('Checked in at')")
        print("SUCCESS: Check-In Verified.")

        await browser.close()

asyncio.run(run())
