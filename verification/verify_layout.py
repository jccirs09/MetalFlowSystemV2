import asyncio
from playwright.async_api import async_playwright

async def run():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(ignore_https_errors=True)
        page = await context.new_page()

        print("Navigating to Home...")
        try:
            await page.goto("http://localhost:5048/", timeout=60000)
        except Exception as e:
            print(f"Failed to load home: {e}")
            await browser.close()
            return

        print(f"Title: {await page.title()}")

        # Try /Account/Login (Blazor Identity default)
        login_url = "http://localhost:5048/Account/Login"
        print(f"Navigating to Login: {login_url}")
        await page.goto(login_url)

        print("Filling Login...")
        try:
            # Wait for selector with a longer timeout
            await page.wait_for_selector("input[type='text'], input[name='Input.Email']", timeout=10000)

            # Fill email
            await page.fill("input[name='Input.Email']", "admin@metalflow.com")
            await page.fill("input[name='Input.Password']", "Admin123!")
            await page.click("button[type='submit']")

            await page.wait_for_url("http://localhost:5048/", timeout=15000)
            print("Login successful, redirected to Home.")

            print("Navigating to Admin Items...")
            await page.goto("http://localhost:5048/admin/items")

            # Wait for the "Item Master" text which is inside the AdminShell -> Items.razor
            await page.wait_for_selector("h5:has-text('Item Master')", timeout=15000)
            print("SUCCESS: Admin Items page loaded and rendered.")

        except Exception as e:
            print(f"FAILURE: {e}")
            await page.screenshot(path="verification/failure_nav_fix.png")
            print("Screenshot saved to verification/failure_nav_fix.png")
            # Dump content to a file for review
            with open("verification/failure_dump.html", "w") as f:
                f.write(await page.content())
            print("HTML Dump saved to verification/failure_dump.html")

        await browser.close()

asyncio.run(run())
