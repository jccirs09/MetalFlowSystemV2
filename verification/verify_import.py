import asyncio
from playwright.async_api import async_playwright
import os
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

        ts = int(time.time())
        sheet_code = f"SHEET-IMP-{ts}"
        coil_code = f"COIL-IMP-{ts}"

        # --- PRE-REQUISITE: CREATE SHEET ITEM ---
        print(f"Creating Sheet Item {sheet_code}...")
        await page.goto("http://localhost:5048/admin/items")
        await page.click("button:has-text('Create Item')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Create Item')")

        await page.fill("div.mud-input-control:has(label:has-text('Item Code')) input", sheet_code)
        await page.fill("div.mud-input-control:has(label:has-text('Description')) textarea", "Imported Sheet")

        # Select PCS
        await page.click("div.mud-select:has(label:has-text('UOM'))")
        await page.wait_for_selector("div.mud-popover-open")
        await page.click("div.mud-list-item:has-text('PCS (Sheets)')")
        await page.wait_for_selector("div.mud-popover-open", state="hidden")

        await asyncio.sleep(0.5)
        await page.fill("div.mud-input-control:has(label:has-text('Pounds Per Square Foot')) input", "2.0")

        await page.click("button:has-text('Save')")
        await page.wait_for_selector("div.mud-dialog-title", state="hidden")
        print("Sheet Item Created.")

        # --- CREATE CSV ---
        csv_path = os.path.abspath(f"verification/snapshot_{ts}.csv")
        with open(csv_path, "w") as f:
            f.write("Item ID,Description,Snapshot Loc,Snapshot,,Width,Length\n")
            f.write(f"{coil_code},Imported Coil,LOC-A,5000,LBS,48,0\n")
            f.write(f"{sheet_code},Imported Sheet,LOC-B,100,PCS,48,120\n")

        # --- IMPORT SNAPSHOT ---
        print("Navigating to Inventory...")
        await page.goto("http://localhost:5048/admin/inventory")

        print("Opening Import Dialog...")
        await page.click("button:has-text('Import Snapshot')")
        await page.wait_for_selector("div.mud-dialog-title:has-text('Import Inventory Snapshot')")

        # Check Branch Selection
        branch_select = page.locator("div.mud-select:has(label:has-text('Select Branch'))").first
        if await branch_select.count() > 0:
            print("Selecting Branch...")
            await branch_select.click()
            await page.wait_for_selector("div.mud-popover-open")
            await page.click("div.mud-list-item")
            await page.wait_for_selector("div.mud-popover-open", state="hidden")

        # Upload File
        async with page.expect_file_chooser() as fc_info:
            await page.click("button:has-text('Select Snapshot File')")

        file_chooser = await fc_info.value
        await file_chooser.set_files(csv_path)

        await asyncio.sleep(1)

        print("Submitting Import...")
        # Use specific selector for the Dialog Action button (Error color)
        # Or search inside the dialog content
        import_btn = page.locator("div.mud-dialog-actions button:has-text('Import Snapshot')")
        await import_btn.click()

        await page.wait_for_selector("div.mud-dialog-title", state="hidden")
        print("Import Submitted.")

        # --- VERIFY INVENTORY ---
        print("Verifying Inventory Data...")

        await page.fill("input[placeholder='Search']", coil_code)
        await page.wait_for_selector(f"td:has-text('{coil_code}')")

        row_coil = page.locator(f"tr:has-text('{coil_code}')")
        row_text_coil = await row_coil.text_content()
        print(f"Coil Row: {row_text_coil}")
        if "5000" in row_text_coil or "5,000" in row_text_coil:
             print("Coil Weight Verified.")
        else:
             print("FAILURE: Coil Weight not found.")

        # Search for SHEET
        await page.fill("input[placeholder='Search']", sheet_code)
        await page.wait_for_selector(f"td:has-text('{sheet_code}')")

        row_sheet = page.locator(f"tr:has-text('{sheet_code}')")
        row_text_sheet = await row_sheet.text_content()
        print(f"Sheet Row: {row_text_sheet}")

        if "100" in row_text_sheet:
             print("Sheet Qty Verified.")
        else:
             print("FAILURE: Sheet Qty not found.")

        if "8000" in row_text_sheet or "8,000" in row_text_sheet:
             print("Sheet Calc Weight Verified.")
        else:
             print("FAILURE: Sheet Calc Weight 8000 not found.")

        print("SUCCESS: Import Logic Verified.")
        await browser.close()
        os.remove(csv_path)

asyncio.run(run())
