from typing import Mapping


def missing_fields(form: Mapping[str, str], required: tuple[str, ...]) -> list[str]:
    return [name for name in required if not form.get(name, "").strip()]


def email_error(form: Mapping[str, str]) -> str | None:
    if "email" in missing_fields(form, ("email",)):
        return "No e-mail address was received. Please enter one."
    if "@" not in form["email"]:
        return "That e-mail address is not valid."
    return None
