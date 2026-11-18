from forms.validate import email_error


def test_typo_fixed():
    assert email_error({}) == "No e-mail address was received. Please enter one."


def test_invalid_unchanged():
    assert email_error({"email": "nope"}) == "That e-mail address is not valid."
