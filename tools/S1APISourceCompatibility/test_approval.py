import unittest

from check import approved


class OwnerApprovalTests(unittest.TestCase):
    command = "/approve-source-break head-a report-a"

    def matches(self, author, body):
        return approved([[], [{"user": {"id": author}, "body": body}]], self.command)

    def test_owner_can_approve_exact_report_on_later_page(self):
        self.assertTrue(self.matches(114284668, self.command))

    def test_approval_is_bound_to_owner_revision_and_report(self):
        for author, body in [
            (1, self.command),
            (114284668, self.command.replace("head-a", "head-b")),
            (114284668, self.command.replace("report-a", "report-b")),
            (114284668, "Example:\n" + self.command),
        ]:
            with self.subTest(author=author, body=body):
                self.assertFalse(self.matches(author, body))


if __name__ == "__main__":
    unittest.main()
