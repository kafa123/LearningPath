import re

DEFAULT_PAGE = 1
DEFAULT_PAGE_SIZE = 20
MAX_PAGE = 10000
MAX_PAGE_SIZE = 100
DIGITS = re.compile(r"^[0-9]+$")


def _read(raw: str | None, default: int) -> tuple[int, bool]:
    if raw is None or not raw.strip():
        return default, True

    text = raw.strip()

    if not DIGITS.fullmatch(text):
        return 0, False

    return int(text), True


def resolve_page(page: str | None, page_size: str | None) -> dict:
    page_value, page_ok = _read(page, DEFAULT_PAGE)

    if not page_ok or not 1 <= page_value <= MAX_PAGE:
        return {
            "isValid": False,
            "page": 0,
            "pageSize": 0,
            "offset": 0,
            "errorCode": "invalid_page",
        }

    page_size_value, page_size_ok = _read(page_size, DEFAULT_PAGE_SIZE)

    if not page_size_ok or not 1 <= page_size_value <= MAX_PAGE_SIZE:
        return {
            "isValid": False,
            "page": 0,
            "pageSize": 0,
            "offset": 0,
            "errorCode": "invalid_page_size",
        }

    return {
        "isValid": True,
        "page": page_value,
        "pageSize": page_size_value,
        "offset": (page_value - 1) * page_size_value,
        "errorCode": None,
    }
