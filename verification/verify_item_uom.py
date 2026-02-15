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

        print("Navigating to Admin Items...")
        await page.goto("http://localhost:5048/admin/items")
        await page.wait_for_selector("h5:has-text('Item Master')")

        # --- TEST 1: CREATE SHEET (PCS) ---
        print("\n--- TEST 1: CREATE SHEET (PCS) ---")
        await page.click("button:has-text('Create Item')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Item')")

        # Fill Basic Info
        item_code_pcs = f"SHEET-TEST-{int(time.time())}"
        await page.fill("div.mud-input-control:has(label:has-text('Item Code')) input", item_code_pcs)
        await page.fill("div.mud-input-control:has(label:has-text('Description')) textarea", "Test Sheet Item")

        # Switch to PCS
        print("Switching to PCS...")
        await page.click("div.mud-select:has(label:has-text('UOM'))")
        await page.wait_for_selector("div.mud-popover-open")
        await page.click("div.mud-list-item:has-text('PCS (Sheets)')")
        await page.wait_for_selector("div.mud-popover-open", state="hidden")

        # Wait a bit for the delay hack
        await asyncio.sleep(0.5)

        # Check PPSF - Should be Enabled
        ppsf_input = page.locator("div.mud-input-control:has(label:has-text('Pounds Per Square Foot')) input")
        is_disabled = await ppsf_input.is_disabled()
        print(f"PPSF Disabled (PCS): {is_disabled}")
        if is_disabled:
             print("FAILURE: PPSF should be enabled for PCS.")

        # Fill PPSF
        await ppsf_input.fill("1.25")

        # Submit
        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden") # Dialog closes

        # Verify in Table
        print("Verifying in table...")
        await page.fill("input[placeholder='Search']", item_code_pcs)
        await page.wait_for_selector(f"td:has-text('{item_code_pcs}')")

        # Check Type Column (Should be Sheet)
        row = page.locator(f"tr:has-text('{item_code_pcs}')")
        type_text = await row.locator("td").nth(2).text_content() # 3rd column is Type
        print(f"Row Type: {type_text.strip()}")
        if "Sheet" not in type_text:
             print("FAILURE: Item Type should be Sheet.")

        # --- TEST 2: CREATE COIL (LBS) ---
        print("\n--- TEST 2: CREATE COIL (LBS) ---")
        await page.click("button:has-text('Create Item')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Item')")

        item_code_lbs = f"COIL-TEST-{int(time.time())}"
        await page.fill("div.mud-input-control:has(label:has-text('Item Code')) input", item_code_lbs)
        await page.fill("div.mud-input-control:has(label:has-text('Description')) textarea", "Test Coil Item")

        # UOM is LBS by default. PPSF should be disabled.
        is_disabled_lbs = await ppsf_input.is_disabled()
        print(f"PPSF Disabled (LBS Default): {is_disabled_lbs}")
        if not is_disabled_lbs:
             print("FAILURE: PPSF should be disabled for LBS.")

        # Submit
        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")

        # Verify
        await page.fill("input[placeholder='Search']", item_code_lbs)
        await page.wait_for_selector(f"td:has-text('{item_code_lbs}')")

        row_lbs = page.locator(f"tr:has-text('{item_code_lbs}')")
        type_text_lbs = await row_lbs.locator("td").nth(2).text_content()
        print(f"Row Type: {type_text_lbs.strip()}")
        if "Coil" not in type_text_lbs:
             print("FAILURE: Item Type should be Coil.")

        print("\nSUCCESS: Data Logic verified.")
        await browser.close()

asyncio.run(run())
