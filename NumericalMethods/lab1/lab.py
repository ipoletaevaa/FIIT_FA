import sys

from iterative import iterative_methods
from jacobi_eigen import jacobi_method
from lu import solve_system
from qr import qr_method
from thomas import thomas_method


def read_task(path):
    with open(path, encoding="utf-8") as source:
        tokens = source.read().split()
    if len(tokens) < 2:
        raise ValueError("в файле нет метода и размера матрицы")
    method = tokens[0]
    n = int(tokens[1])
    numbers = []
    for token in tokens[2:]:
        numbers.append(float(token))
    return method, n, numbers


def take(numbers, count, pos):
    if pos + count > len(numbers):
        raise ValueError("в файле не хватает чисел")
    return numbers[pos:pos + count], pos + count


def take_matrix(numbers, n, pos):
    matrix = []
    for _ in range(n):
        row, pos = take(numbers, n, pos)
        matrix.append(row)
    return matrix, pos


def format_number(value):
    if isinstance(value, complex):
        real = 0.0 if abs(value.real) < 1e-9 else value.real
        imag = 0.0 if abs(value.imag) < 1e-9 else value.imag
        if imag == 0.0:
            return f"{real:12.6g}"
        sign = "+" if imag > 0.0 else "-"
        return f"{real:.6g} {sign} {abs(imag):.6g}i"
    if abs(value) < 1e-9:
        value = 0.0
    return f"{value:12.6g}"


def print_vector(title, values):
    print(title)
    parts = []
    for value in values:
        parts.append(format_number(value).strip())
    print("  " + "  ".join(parts))
    print()


def print_matrix(title, matrix):
    print(title)
    for row in matrix:
        parts = []
        for value in row:
            parts.append(format_number(value))
        print("  " + " ".join(parts))
    print()


def print_lu(a, b):
    result = solve_system(a, b)
    print("Метод: LU с выбором главного элемента в столбце")
    print()
    print_matrix("L =", result["L"])
    print_matrix("U =", result["U"])
    print_matrix("L·U =", result["LU"])
    print_vector("Решение x =", result["x"])
    print_matrix("Обратная матрица A⁻¹ =", result["inverse"])
    print(f"Число перестановок строк: {result['swaps']}")
    print(f"Определитель det A = {format_number(result['det']).strip()}")
    print("Формула: det A = (-1)^(число перестановок) * произведение диагонали U")
    print()
    print_matrix("Проверка A·A⁻¹ =", result["check_inverse"])
    print_matrix("Проверка L·U =", result["LU"])
    print_matrix("Проверка P·A =", result["PA"])


def print_thomas(lower, main, upper, rhs):
    alpha, beta, x = thomas_method(lower, main, upper, rhs)
    print("Метод: прогонка")
    print()
    print("Прогоночные коэффициенты:")
    for i in range(len(alpha)):
        print(
            f"  α{i + 1} = {format_number(alpha[i]).strip()}"
            f"    β{i + 1} = {format_number(beta[i]).strip()}"
        )
    print()
    print_vector("Решение x =", x)


def print_iterative(a, b, eps):
    result = iterative_methods(a, b, eps)
    print("Метод: простые итерации и метод Зейделя")
    print()
    print(f"ε = {format_number(result['eps']).strip()}")
    print(f"Простые итерации: {result['simple_iters']} итераций")
    print_vector("Решение простых итераций:", result["simple_x"])
    print(f"Метод Зейделя: {result['seidel_iters']} итераций")
    print_vector("Решение метода Зейделя:", result["seidel_x"])
    print_vector("Эталонное решение через LU:", result["reference"])
    print(f"Быстрее сошёлся: {result['faster']}")
    print()


def print_jacobi(a, eps):
    result = jacobi_method(a, eps)
    print("Метод: вращения Якоби")
    print()
    print(f"ε = {format_number(result['eps']).strip()}")
    print_vector("Собственные значения:", result["eigenvalues"])
    print_matrix("Матрица собственных векторов V =", result["V"])
    print_matrix("Проверка A·V =", result["AV"])
    print_matrix("Проверка V·Λ =", result["V_lambda"])
    print("Погрешность по итерациям (наибольший внедиагональный элемент):")
    print("  итерация    погрешность")
    for index, error in enumerate(result["errors"]):
        print(f"  {index:8d}    {format_number(error).strip()}")
    print()


def print_qr(a, eps):
    result = qr_method(a, eps)
    print("Метод: QR-разложение и QR-алгоритм")
    print()
    print_matrix("A =", result["A"])
    print_matrix("Q =", result["Q"])
    print_matrix("R =", result["R"])
    print_matrix("Q·R =", result["QR"])
    print_vector("Собственные значения:", result["eigenvalues"])
    if result["numpy"] is None:
        print("NumPy не установлен, сверка numpy.linalg.eigvals пропущена.")
    else:
        print_vector("Сверка numpy.linalg.eigvals:", result["numpy"])
    print()


def main():
    if len(sys.argv) != 2:
        print("Запуск: python3 lab.py <файл>")
        print("Пример: python3 lab.py examples/variants/01.txt")
        return 1

    method, n, numbers = read_task(sys.argv[1])
    pos = 0

    if method == "lu":
        a, pos = take_matrix(numbers, n, pos)
        b, pos = take(numbers, n, pos)
        print_lu(a, b)
    elif method == "thomas":
        lower, pos = take(numbers, n, pos)
        main_diag, pos = take(numbers, n, pos)
        upper, pos = take(numbers, n, pos)
        rhs, pos = take(numbers, n, pos)
        print_thomas(lower, main_diag, upper, rhs)
    elif method == "iterative":
        a, pos = take_matrix(numbers, n, pos)
        b, pos = take(numbers, n, pos)
        eps_values, pos = take(numbers, 1, pos)
        print_iterative(a, b, eps_values[0])
    elif method == "jacobi":
        a, pos = take_matrix(numbers, n, pos)
        eps_values, pos = take(numbers, 1, pos)
        print_jacobi(a, eps_values[0])
    elif method == "qr":
        a, pos = take_matrix(numbers, n, pos)
        eps_values, pos = take(numbers, 1, pos)
        print_qr(a, eps_values[0])
    else:
        raise ValueError("неизвестный метод: " + method)

    if pos != len(numbers):
        raise ValueError("в файле есть лишние числа")
    return 0


if __name__ == "__main__":
    sys.exit(main())
