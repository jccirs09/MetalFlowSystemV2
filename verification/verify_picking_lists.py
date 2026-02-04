import time
from playwright.sync_api import sync_playwright, expect

def test_picking_lists(page):
    print("Navigating to Login...")
    page.goto("http://localhost:5000/Account/Login")

    # Login
    print("Logging in...")
    page.fill("input[name='Input.Email']", "admin@metalflow.com")
    page.fill("input[name='Input.Password']", "Admin123!")
    page.click("button[type='submit']")

    # Wait for navigation
    print("Waiting for dashboard...")
    page.wait_for_url("http://localhost:5000/")

    # Navigate to Picking Lists (WASM Page)
    print("Navigating to Picking Lists...")
    page.goto("http://localhost:5000/picking-lists")

    # Wait for loading to finish (Table should appear)
    # The page has a title "Picking Lists"
    print("Waiting for page load...")
    expect(page.get_by_role("heading", name="Picking Lists")).to_be_visible(timeout=10000)

    # Take screenshot
    print("Taking screenshot...")
    page.screenshot(path="verification/picking_lists.png")
    print("Done.")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_picking_lists(page)
        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/picking_lists_error.png")
        finally:
            browser.close()
