import unittest
from pathlib import Path

import numpy as np

from iterative import iterative_methods
from lu import solve_system
from qr import qr_method
from thomas import thomas_method
import lab


ROOT = Path(__file__).resolve().parents[1]


def read_variant(number):
    path = ROOT / "examples" / "variants" / f"{number:02d}.txt"
    method, n, numbers = lab.read_task(path)
    matrix, pos = lab.take_matrix(numbers, n, 0)
    rhs, pos = lab.take(numbers, n, pos)
    return matrix, rhs


class LabTests(unittest.TestCase):
    def test_variant01_solution_determinant_and_lu(self):
        matrix, rhs = read_variant(1)
        result = solve_system(matrix, rhs)
        self.assertTrue(np.allclose(result["x"], [2, 4, 2, 3], atol=1e-8))
        self.assertAlmostEqual(result["det"], 8.0, places=6)
        self.assertTrue(np.allclose(result["LU"], result["PA"], atol=1e-8))
        self.assertTrue(np.allclose(result["check_inverse"], np.eye(4), atol=1e-8))

    def test_all_variants_match_permutation(self):
        for number in range(1, 31):
            matrix, rhs = read_variant(number)
            result = solve_system(matrix, rhs)
            self.assertTrue(np.allclose(result["LU"], result["PA"], atol=1e-6))
            product = np.array(matrix, dtype=float) @ np.array(result["x"])
            self.assertTrue(np.allclose(product, rhs, atol=1e-6))

    def test_thomas_gives_ones(self):
        _, n, numbers = lab.read_task(ROOT / "examples" / "thomas.txt")
        lower, pos = lab.take(numbers, n, 0)
        main, pos = lab.take(numbers, n, pos)
        upper, pos = lab.take(numbers, n, pos)
        rhs, _ = lab.take(numbers, n, pos)
        _, _, x = thomas_method(lower, main, upper, rhs)
        self.assertTrue(np.allclose(x, [1, 1, 1, 1, 1], atol=1e-8))

    def test_seidel_is_faster_and_solution_is_ones(self):
        _, n, numbers = lab.read_task(ROOT / "examples" / "iterative.txt")
        matrix, pos = lab.take_matrix(numbers, n, 0)
        rhs, pos = lab.take(numbers, n, pos)
        eps = lab.take(numbers, 1, pos)[0][0]
        result = iterative_methods(matrix, rhs, eps)
        self.assertLess(result["seidel_iters"], result["simple_iters"])
        self.assertTrue(np.allclose(result["seidel_x"], [1, 1, 1], atol=1e-6))
        self.assertTrue(np.allclose(result["simple_x"], [1, 1, 1], atol=1e-6))
        self.assertTrue(np.allclose(result["reference"], [1, 1, 1], atol=1e-8))

    def test_qr_complex_pair_matches_numpy(self):
        _, n, numbers = lab.read_task(ROOT / "examples" / "qr_complex.txt")
        matrix, pos = lab.take_matrix(numbers, n, 0)
        eps = lab.take(numbers, 1, pos)[0][0]
        result = qr_method(matrix, eps)
        own = np.array(result["eigenvalues"])
        reference = np.linalg.eigvals(np.array(matrix, dtype=float))
        self.assertEqual(len(own), len(reference))
        for value in own:
            distances = np.abs(reference - value)
            self.assertLess(distances.min(), 1e-8)
        self.assertTrue(np.allclose(result["QR"], matrix, atol=1e-8))


if __name__ == "__main__":
    unittest.main()
