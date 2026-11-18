from forms.validate import email_error, missing_fields


def test_valid():
    assert email_error({"email": "a@b.c"}) is None


def test_missing_fields():
    assert missing_fields({"a": " "}, ("a", "b")) == ["a", "b"]


def test_missing_email_message_spelling():
    assert email_error({"email": " "}) == "No e-mail address was received. Please enter one."
